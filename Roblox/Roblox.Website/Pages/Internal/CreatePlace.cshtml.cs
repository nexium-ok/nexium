using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Logging;
using Roblox.Models.Assets;
using Roblox.Models.Economy;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Pages.Internal;

public class CreatePlace : RobloxPageModel
{
    public string? errorMessage { get; set; }
    public string? successUrl { get; set; }

    private const int PlaceCreationCost = 350;

    public void OnGet() { }

    public async Task OnPost()
    {
        if (userSession == null)
        {
            errorMessage = "Not logged in.";
            return;
        }

        if (!FeatureFlags.IsEnabled(FeatureFlag.CreatePlaceSelfService))
        {
            errorMessage = "Place creation is disabled globally at this time. Try again later.";
            return;
        }

        await using var createGameLock =
            await Roblox.Services.Cache.redLock.CreateLockAsync(
                "CreatePlaceSelfServiceV1:UserId:" + userSession.userId,
                TimeSpan.FromSeconds(10));

        if (!createGameLock.IsAcquired)
        {
            Writer.Info(LogGroup.AbuseDetection, "CreatePlace OnPost could not acquire createGameLock");
            errorMessage = "Too many attempts. Try again in a few seconds.";
            return;
        }

        // check if user has enough robux
        var economy = await services.economy.GetUserBalance(userSession.userId);
        if (economy.robux < PlaceCreationCost)
        {
            errorMessage = $"You do not have enough Robux. Place creation costs {PlaceCreationCost} Robux.";
            return;
        }

        // take away robux
        await services.economy.DecrementCurrency(userSession.userId, CurrencyType.Robux, PlaceCreationCost);

        // log the transaction
        await services.users.InsertAsync("user_transaction", new
        {
            type = PurchaseType.Purchase,
            currency_type = CurrencyType.Robux,
            amount = PlaceCreationCost,
            sub_type = TransactionSubType.ItemPurchase,
            user_id_one = userSession.userId,
            user_id_two = 1,
        });

        Writer.Info(LogGroup.AbuseDetection,
            "CreatePlace OnPost userId={0} deducted {1} Robux, creating place",
            userSession.userId, PlaceCreationCost);

        // create place and universe
        var asset = await services.assets.CreatePlace(userSession.userId, CreatorType.User, userSession.userId);
        await services.games.CreateUniverse(asset.placeId);

        successUrl = "https://nexrev.org/internal/place-update?id=" + asset.placeId;
    }
}