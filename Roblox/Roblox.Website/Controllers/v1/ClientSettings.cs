using Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.FastFlag;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/clientsettings/v1")]
public class ClientSettingsControllerV1 : ControllerBase
{
    // e.g. GET /apisite/clientsettings/v1/settings/application/PCDesktopClient
    [HttpGet("settings/application/{applicationName}")]
    public Dictionary<string, dynamic> GetApplicationSettings(string applicationName)
    {
        return DefaultClientSettings.GetFlags(applicationName).ToDictionary();
    }
}