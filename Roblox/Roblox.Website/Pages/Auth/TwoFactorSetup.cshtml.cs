using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Services;
using Roblox.Website.Controllers;

namespace Roblox.Website.Pages.Auth;

public class TwoFactorSetup : RobloxPageModel
{
    private const string EmptyCodeMessage = "The code cannot be blank, please insert the code";
    private const string BadCodeMessage = "This code is incorrect, please make sure to copy the code your authenticator gives you";

    [BindProperty(SupportsGet = true)]
    public string token { get; set; }

    [BindProperty]
    public string? code { get; set; }

    public string? secret { get; set; }
    public bool tokenInvalid { get; set; }
    public string? errorMessage { get; set; }

    public async Task OnGet()
    {
        secret = await services.twoFactor.GetSecretByToken(token);
        if (secret == null)
        {
            tokenInvalid = true;
        }
    }

    public async Task<IActionResult> OnPost()
    {
        secret = await services.twoFactor.GetSecretByToken(token);
        if (secret == null)
        {
            tokenInvalid = true;
            return new PageResult();
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            errorMessage = EmptyCodeMessage;
            return new PageResult();
        }

        var isValid = await services.twoFactor.VerifyCodeByToken(token, code);
        if (!isValid)
        {
            errorMessage = BadCodeMessage;
            return new PageResult();
        }

        var completed = await services.twoFactor.CompleteSetup(token);
        if (!completed)
        {
            tokenInvalid = true;
            return new PageResult();
        }

        return new RedirectResult("/My/Account");
    }
}