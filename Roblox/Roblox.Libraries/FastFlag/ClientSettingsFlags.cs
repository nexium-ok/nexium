namespace Roblox.Libraries.FastFlag;

// this is so you can modify clientsettings fastflags

public static class DefaultClientSettings
{
    public static FastFlagResult GetFlags(string applicationName)
    {
        var appName = applicationName?.ToLowerInvariant() ?? "";
        var isFirefox = appName.Contains("firefox");
        var isChrome = !isFirefox && appName.Contains("chrome");
        var isRcc = appName.Contains("rcc");

        return new FastFlagResult().AddFlags(new List<IFastFlag>
        {
            new FastFlag("FlagsLoaded", true),
            new DFastFlag("SDLRelativeMouseEnabled", true),
            new DFastFlag("SDLMousePanningFixed", true),
            new DFastFlag("OLResetPositionOnLoopStart", true, isChrome),
            new DFastFlag("OLIgnoreErrors", false),
            new FastFlag("Is18OrOverEnabled", true),
            new FastFlag("KickEnabled", true),
        });
    }
}