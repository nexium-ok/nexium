using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Dapper;
using OtpNet;

namespace Roblox.Services
{
    public class TwoFactorService : ServiceBase, IService
    {
        // sorry about using user_email, will fix later
        private const int TwoFactorStatusCode = 1;
        private const int PendingStatusCode = 0;

        private const string TokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static string GenerateSetupToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            var chars = new char[20];
            for (var i = 0; i < 20; i++)
            {
                chars[i] = TokenChars[bytes[i] % TokenChars.Length];
            }
            return new string(chars);
        }

        private async Task<(long Id, long UserId, string Secret, string? SetupToken, int Status)?> GetRow(long userId)
        {
            var record = await db.QuerySingleOrDefaultAsync<(long id, long user_id, string secret, string? setup_token, int status)?>(
                "SELECT id, user_id, email AS secret, setup_token, status FROM user_email WHERE user_id = :user_id",
                new { user_id = userId });
            return record;
        }

        private async Task<(long Id, long UserId, string Secret, string? SetupToken, int Status)?> GetRowByToken(string token)
        {
            var record = await db.QuerySingleOrDefaultAsync<(long id, long user_id, string secret, string? setup_token, int status)?>(
                "SELECT id, user_id, email AS secret, setup_token, status FROM user_email WHERE setup_token = :token AND status = :status",
                new { token, status = PendingStatusCode });
            return record;
        }

        public async Task<bool> IsEnabled(long userId)
        {
            var exists = await db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM user_email WHERE user_id = :user_id AND status = :status)",
                new { user_id = userId, status = TwoFactorStatusCode });
            return exists;
        }

        /// <summary>
        /// Returns the setup token for this user's in-progress 2FA setup, creating a new
        /// secret + token if none exists yet. Does not create a new one if setup is already
        /// pending (keeps the /auth/2fa/{token} link stable across page loads).
        /// Throws if 2FA is already enabled - check IsEnabled() first.
        /// </summary>
        public async Task<string> GetOrCreateSetupToken(long userId)
        {
            var existing = await GetRow(userId);
            if (existing.HasValue)
            {
                if (existing.Value.Status == TwoFactorStatusCode)
                {
                    throw new InvalidOperationException("2FA is already enabled for this user");
                }
                if (existing.Value.SetupToken != null)
                {
                    return existing.Value.SetupToken;
                }
            }

            var key = KeyGeneration.GenerateRandomKey(40);
            var secret = Base32Encoding.ToString(key);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var token = GenerateSetupToken();
                try
                {
                    if (existing.HasValue)
                    {
                        await db.ExecuteAsync(
                            "UPDATE user_email SET email = :secret, setup_token = :token, updated_at = NOW() WHERE user_id = :user_id",
                            new { user_id = userId, secret, token });
                    }
                    else
                    {
                        await db.ExecuteAsync(
                            "INSERT INTO user_email (user_id, email, status, setup_token, created_at, updated_at) VALUES (:user_id, :secret, :status, :token, NOW(), NOW())",
                            new { user_id = userId, secret, status = PendingStatusCode, token });
                    }
                    return token;
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                {
                    // setup_token collision (astronomically unlikely) - retry with a new token
                }
            }

            throw new Exception("Could not generate a unique 2FA setup token");
        }

        /// <summary>
        /// Looks up the secret for an in-progress (not yet enabled) setup by its token.
        /// Returns null if the token is invalid, expired, or has already been used.
        /// </summary>
        public async Task<string?> GetSecretByToken(string token)
        {
            var row = await GetRowByToken(token);
            return row?.Secret;
        }

        public async Task<bool> VerifyCodeByToken(string token, string code)
        {
            var row = await GetRowByToken(token);
            if (!row.HasValue)
            {
                return false;
            }
            return VerifyTotpCode(row.Value.Secret, code);
        }

        /// <summary>
        /// Marks 2FA as enabled for the account tied to this token, and invalidates the
        /// (now used) setup token. Returns false if the token was invalid/already used.
        /// </summary>
        public async Task<bool> CompleteSetup(string token)
        {
            var updated = await db.ExecuteAsync(
                "UPDATE user_email SET status = :enabled, setup_token = NULL, updated_at = NOW() WHERE setup_token = :token AND status = :pending",
                new { enabled = TwoFactorStatusCode, token, pending = PendingStatusCode });
            return updated > 0;
        }

        public async Task<bool> VerifyCode(long userId, string code)
        {
            var row = await GetRow(userId);
            if (!row.HasValue)
            {
                Console.WriteLine($"no 2FA TOTP found for {userId}");
                return false;
            }
            return VerifyTotpCode(row.Value.Secret, code);
        }

        /// <summary>
        /// Admin-only: wipes 2FA (enabled or pending) for a user entirely.
        /// </summary>
        public async Task Disable2FA(long userId)
        {
            await db.ExecuteAsync(
                "DELETE FROM user_email WHERE user_id = :user_id",
                new { user_id = userId });
        }

        private bool VerifyTotpCode(string secret, string code)
        {
            secret = secret.ToUpper().Replace(" ", "").Trim();

            try
            {
                var bytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(bytes);

                var verTime = GetAccurateTime();
                var verWindow = new VerificationWindow(1, 1);
                var result = totp.VerifyTotp(verTime.ToUniversalTime(), code, out var matchedTimestep, verWindow);
                Console.WriteLine($"2fa result: {result}, matched: {matchedTimestep}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error in TOTP verification: {ex}");
                return false;
            }
        }

        private static TimeSpan? NtpOffset = null;
        private static DateTime LastNtp;

        private static DateTime GetAccurateTime()
        {
            if (NtpOffset == null || (DateTime.UtcNow - LastNtp).TotalMinutes > 15)
            {
                try
                {
                    var networkTime = NtpClient.GetNetworkTime();
                    NtpOffset = networkTime.ToUniversalTime() - DateTime.UtcNow;
                    LastNtp = DateTime.UtcNow;
                }
                catch
                {
                    NtpOffset = TimeSpan.Zero;
                }
            }

            return DateTime.UtcNow + (NtpOffset ?? TimeSpan.Zero);
        }

        private static class NtpClient
        {
            private const string NtpServer = "pool.ntp.org";
            private const int NtpPort = 123;

            public static DateTime GetNetworkTime()
            {
                var ntpData = new byte[48];
                ntpData[0] = 0x1B;

                var addresses = Dns.GetHostEntry(NtpServer).AddressList;
                var IP = new IPEndPoint(addresses[0], NtpPort);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect(IP);
                    socket.Send(ntpData);
                    socket.ReceiveTimeout = 3000;
                    socket.Receive(ntpData);
                    socket.Close();
                }

                const byte serverReplyTime = 40;

                ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
                ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                var networkDateTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);

                return networkDateTime.ToLocalTime();
            }

            private static uint SwapEndianness(ulong x)
            {
                return (uint)(((x & 0x000000ff) << 24) +
                              ((x & 0x0000ff00) << 8) +
                              ((x & 0x00ff0000) >> 8) +
                              ((x & 0xff000000) >> 24));
            }
        }
        public bool IsThreadSafe() => true;
        public bool IsReusable() => false;
    }
}