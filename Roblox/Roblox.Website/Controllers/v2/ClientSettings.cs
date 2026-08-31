using Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.FastFlag;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/clientsettings/v2")]
public class ClientSettingsControllerV2 : ControllerBase
{
    // e.g. GET /apisite/clientsettings/v2/settings/application/PCDesktopClient
    [HttpGet("settings/application/{applicationName}")]
    public dynamic GetApplicationSettings(string applicationName)
    {
        return new
        {
            applicationSettings = DefaultClientSettings.GetFlags(applicationName).ToDictionary(),
        };
    }

    
    [HttpGet("user-settings/application/{applicationName}")]
    public dynamic GetUserApplicationSettings(string applicationName)
    {
        return new
        {
            applicationSettings = DefaultClientSettings.GetFlags(applicationName).ToDictionary(),
        };
    }
}