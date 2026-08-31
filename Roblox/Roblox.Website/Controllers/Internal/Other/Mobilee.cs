using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Models.Chat;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;
using BadRequestException = Roblox.Exceptions.BadRequestException;

namespace Roblox.Website.Controllers
{
    [ApiController]
    [Route("/")]
    public class Mobilee : ControllerBase
    {
        public class LoginRequestV1
        {
            public string cvalue { get; set; } = "";
            public string password { get; set; } = "";
        }

        public class AddToConversationRequest
        {
            public long conversationId { get; set; }
            public List<long> participantUserIds { get; set; } = new();
        }

        public class StartCloudeditConversationRequest
        {
            public long placeId { get; set; }
        }

        [HttpPostBypass("v1/login")]
        public async Task<dynamic> LoginV1([FromBody] LoginRequestV1 request)
        {
            FeatureCheck();
            await RateLimitCheck();

            if (string.IsNullOrEmpty(request.cvalue) || string.IsNullOrEmpty(request.password))
                throw new BadRequestException(3, "Username or password is missing.");

            string[] splittedUsername = request.cvalue.Split('|');
            string username = splittedUsername[0];
            string? totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            UserInfo userInfo;
            try
            {
                var userId = await services.users.GetUserIdFromUsername(username);
                userInfo = await services.users.GetUserById(userId);
            }
            catch (RecordNotFoundException)
            {
                throw new ForbiddenException(1, "Incorrect username or password. Please try again.");
            }

            await DoLogin(userInfo.username, request.password, userInfo.userId, totpCode);
            await CreateSessionAndSetCookie(userInfo.userId);

            return new
            {
                user = new
                {
                    id = userInfo.userId,
                    name = userInfo.username,
                    displayName = userInfo.username,
                },
                isBanned = userInfo.IsDeleted()
            };
        }

        [HttpGetBypass("v1/users/{userId:long}/currency")]
        public async Task<dynamic> GetUserCurrency(long userId)
        {
            return await services.economy.GetUserBalance(safeUserSession.userId);
        }

        [HttpGetBypass("mobile/pbe")]
        [HttpPostBypass("mobile/pbe")]
        [HttpGetBypass("client/pbe")]
        [HttpPostBypass("client/pbe")]
        public OkResult PBE()
        {
            return Ok();
        }

        [HttpGetBypass("v2/metadata")]
        public dynamic GetMetadata()
        {
            return new
            {
                isChatEnabledByPrivacySetting = 0,
                languageForPrivacySettingUnavailable = "Chat is currently unavailable",
                maxConversationTitleLength = 150,
                numberOfMembersForPartyChrome = 6,
                partyChromeDisplayTimeStampInterval = 300000,
                signalRDisconnectionResponseInMilliseconds = 3000,
                typingInChatFromSenderThrottleMs = 5000,
                typingInChatForReceiverExpirationMs = 8000,
                relativeValueToRecordUiPerformance = 0.0,
                isChatDataFromLocalStorageEnabled = false,
                chatDataFromLocalStorageExpirationSeconds = 30,
                isUsingCacheToLoadFriendsInfoEnabled = false,
                cachedDataFromLocalStorageExpirationMS = 30000,
                senderTypesForUnknownMessageTypeError = new List<string>() { "User" },
                isInvalidMessageTypeFallbackEnabled = false,
                isRespectingMessageTypeEnabled = false,
                validMessageTypesWhiteList = new List<string>() { "PlainText", "Link" },
                shouldRespectConversationHasUnreadMessageToMarkAsRead = true,
                isVoiceChatForClientSideEnabled = false,
                isAliasChatForClientSideEnabled = true,
                isPlayTogetherForGameCardsEnabled = true,
                isRoactChatEnabled = true
            };
        }

        [HttpGetBypass("v2/chat-settings")]
        [HttpGetBypass("v1/chat-settings")]
        public dynamic GetChatSettings()
        {
            return new
            {
                chatEnabled = FeatureFlags.IsEnabled(FeatureFlag.WebsiteChat),
                isActiveChatUser = true,
            };
        }

        [HttpGetBypass("v2/get-user-conversations")]
        public async Task<dynamic> GetAuthenticatedUserConversations(int pageNumber, int pageSize)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            if (pageSize > 100 || pageSize < 0)
                throw new RobloxException(400, 0, "BadRequest");
            var offset = pageNumber * pageSize - pageSize;
            var conversations = await services.chat.GetUserConversations(safeUserSession.userId);
            var response = new List<dynamic>();
            foreach (var item in conversations.Skip(offset))
            {
                if (response.Count >= pageSize)
                    break;
                var participants = (await services.chat.GetChatParticipants(item.id)).ToArray();
                var names = (await services.users.MultiGetUsersById(participants.Select(c => c.userId))).ToArray();
                var hasUnRead = await services.chat.DoesHaveUnreadMessages(item.id, safeUserSession.userId);
                if (item.conversationType == ConversationType.OneToOneConversation)
                {
                    var areFriends = await services.friends.AreAlreadyFriends(participants[0].userId, participants[1].userId);
                    if (!areFriends)
                        continue;
                }
                response.Add(new
                {
                    id = item.id,
                    title = item.title,
                    hasUnreadMessages = hasUnRead,
                    participants = participants.Select(c => new
                    {
                        type = "User",
                        targetId = c.userId,
                        name = names.FirstOrDefault(a => a.id == c.userId)?.name,
                    }),
                    conversationType = item.conversationType,
                    conversationTitle = new
                    {
                        titleForViewer = item.title,
                        isDefaultTitle = item.title == null,
                    },
                    conversationUniverse = (object?)null,
                });
            }

            return response;
        }

        [HttpPostBypass("v2/mark-as-read")]
        public async Task MarkMessageAsRead([Required, FromBody] Dto.Chat.MarkAsReadRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            await services.chat.MarkMessageAsRead(request.conversationId, request.endMessageId, safeUserSession.userId);
        }

        [HttpPostBypass("v2/add-to-conversation")]
        public dynamic AddToConversation([Required, FromBody] AddToConversationRequest request)
        {
            // ECS ChatService does not support GetConversation or multi-participant management
            return new
            {
                conversationId = request.conversationId,
                resultType = "Success",
            };
        }

        [HttpPostBypass("v2/start-cloud-edit-conversation")]
        public dynamic StartCloudEditConversation([Required, FromBody] StartCloudeditConversationRequest request)
        {
            // ECS does not support cloud-edit (Studio collaborative) conversations
            return new
            {
                conversation = new { id = 0L },
            };
        }

        [HttpPostBypass("v2-start-one-to-one-conversation")]
        public async Task<dynamic> StartOneToOneConversation([Required, FromBody] Dto.Chat.StartConversationRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            var result = await services.chat.CreateOneToOneConversation(safeUserSession.userId, request.participantUserId);
            return new
            {
                conversation = new
                {
                    id = result.id,
                },
            };
        }

        [HttpPostBypass("v2/update-user-typing-status")]
        public async Task UpdateUserTypingStatus([Required, FromBody] Dto.Chat.UpdateTypingStatusRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            await services.chat.StartTyping(request.conversationId, safeUserSession.userId);
        }

        [HttpPostBypass("v2/send-message")]
        public async Task<dynamic> SendMessage([Required, FromBody] Dto.Chat.SendMessageRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            var resp = await services.chat.SendMessage(request.conversationId, safeUserSession.userId, request.message);
            return new
            {
                content = resp.message,
                messageId = resp.id,
                sent = resp.createdAt,
                messageType = "PlainText",
                resultType = "Success",
            };
        }

        [HttpGetBypass("v2/multi-get-latest-messages")]
        public async Task<dynamic> MultiGetLatestMessages(string conversationIds)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            var ids = conversationIds.Split(",").Select(long.Parse).Distinct().ToArray();
            if (ids.Length == 0 || ids.Length > 100)
                throw new RobloxException(400, 0, "BadRequest");

            var result = new List<dynamic>();
            foreach (var id in ids)
            {
                if (!await services.chat.IsUserInConversation(id, safeUserSession.userId))
                    throw new RobloxException(403, 0, "Forbidden");
                var latest = await services.chat.GetLatestMessageInConversation(id);
                var isRead = latest == null || await services.chat.IsRead(latest.id, id, safeUserSession.userId);

                result.Add(new
                {
                    conversationId = id,
                    chatMessages = latest != null ? new[]
                    {
                        new
                        {
                            id = latest.id,
                            sent = latest.createdAt,
                            read = isRead,
                            senderTargetId = latest.userId,
                            content = latest.message,
                        }
                    } : ArraySegment<dynamic>.Empty,
                });
            }

            return result;
        }

        [HttpGetBypass("v2/get-messages")]
        public async Task<dynamic> GetMessages(long conversationId, int pageSize, string? exclusiveStartMessageId = null)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.WebsiteChat);
            exclusiveStartMessageId ??= "";
            if (pageSize > 100 || pageSize < 0)
                throw new RobloxException(400, 0, "BadRequest");
            if (!await services.chat.IsUserInConversation(conversationId, safeUserSession.userId))
                throw new RobloxException(403, 0, "Forbidden");

            var messages = await services.chat.GetLatestMessagesInConversation(conversationId, exclusiveStartMessageId, pageSize);

            var response = new List<dynamic>();
            foreach (var message in messages)
            {
                response.Add(new
                {
                    id = message.id,
                    senderType = "User",
                    sent = message.createdAt,
                    read = await services.chat.IsRead(message.id, message.conversationId, safeUserSession.userId),
                    messageType = "PlainText",
                    senderTargetId = message.userId,
                    content = message.message,
                });
            }

            return response;
        }

        [HttpGetBypass("v1/enrollments")]
        [HttpPostBypass("v1/enrollments")]
        public dynamic Enrollments()
        {
            return new
            {
                data = new[]
                {
                    new
                    {
                        SubjectType = "BrowserTracker",
                        SubjectTargetId = 63713166375,
                        ExperimentName = "AllUsers.DevelopSplashScreen.GreenStartCreatingButton",
                        Status = "Inactive",
                        Variation = (string?)null
                    }
                }
            };
        }

        [HttpGetBypass("v1/get-enrollments")]
        [HttpPostBypass("v1/get-enrollments")]
        public dynamic GetEnrollments()
        {
            return Array.Empty<object>();
        }

        [HttpGetBypass("v1/themes/User/{userId:long}")]
        public dynamic GetUserTheme(long userId)
        {
            return new { themeType = "Dark" };
        }

        [HttpGetBypass("v2/get-rollout-settings")]
        public dynamic ChatRollout(string featureNames)
        {
            return new
            {
                rolloutFeatures = new[]
                {
                    new
                    {
                        featureName = featureNames,
                        isRolloutEnabled = true
                    }
                }
            };
        }

        [HttpGetBypass("v2/passwords/current-status")]
        public dynamic GetPasswordStatus()
        {
            return new { valid = userSession != null };
        }

        // version check

        [HttpGet("mobileapi/check-app-version")]
        [HttpPost("mobileapi/check-app-version")]
        [HttpGetBypass("mobileapi/check-app-version")]
        [HttpPostBypass("mobileapi/check-app-version")]
        public IActionResult MobileCheckAppVersion()
        {
            return Ok(new
            {
                data = new
                {
                    UpgradeAction = "None"
                }
            });
        }

        // btid
        [HttpGetBypass("device/initialize")]
        [HttpPostBypass("device/initialize")]
        public IActionResult DeviceInitialize()
        {
            return Ok(new
            {
                browserTrackerId = 1,
                appDeviceIdentifier = (string?)null,
            });
        }

        // found out this is for 2016, this is bullshit, does it need this or am i hallucinating? still have to add it cause it 404s

        [HttpGet("sponsoredpage/list-json")]
        [HttpPost("sponsoredpage/list-json")]
        public dynamic SponsoredPageList()
        {
            return new
            {
                Ads = Array.Empty<object>(),
            };
        }

        private void FeatureCheck()
        {
            try
            {
                FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            }
            catch (RobloxException)
            {
                throw new RobloxException(503, 11, "Login is currently disabled. Please try again later.");
            }
        }

        private async Task RateLimitCheck()
        {
            var loginKey = "LoginV1:" + GetIP();
            var attemptCount = (await services.cooldown.GetBucketDataForKey(loginKey, TimeSpan.FromMinutes(10))).ToArray();
            if (!await services.cooldown.TryIncrementBucketCooldown(loginKey, 15, TimeSpan.FromMinutes(10), attemptCount, true))
                throw new ForbiddenException(0, "Too many attempts.");
        }

        private async Task DoLogin(string username, string password, long userId, string? totpCode)
        {
            FeatureCheck();
            await RateLimitCheck();

            try
            {
                if (!await services.users.VerifyPassword(userId, password))
                    throw new ForbiddenException(1, "Incorrect username or password. Please try again");
            }
            catch (RecordNotFoundException)
            {
                throw new ForbiddenException(4, "Your account has been locked. Please reset your password to unlock your account.");
            }

            if (await services.twoFactor.IsEnabled(userId))
            {
                if (string.IsNullOrEmpty(totpCode))
                    throw new ForbiddenException(8, "2FA is enabled. Please login with this username format: username|2FACode");

                if (!await services.twoFactor.VerifyCode(userId, totpCode))
                    throw new ForbiddenException(1, "Incorrect 2FA code. Please try again.");
            }
        }
    }
}