//FROM: https://github.com/nanoframework/Samples/tree/main/samples/WiFiAP

namespace VacuDoor;
internal class Wireless80211
{
    public static bool IsEnabled()
    {
        var wconf = GetConfiguration();
        return !string.IsNullOrEmpty(wconf.Ssid);
    }

    /// <summary>
    /// Disable the Wireless station interface.
    /// </summary>
    public static void Disable()
    {
        var wconf = GetConfiguration();
        wconf.Options = Wireless80211Configuration.ConfigurationOptions.None;
        wconf.SaveConfiguration();
    }

    /// <summary>
    /// Configure and enable the Wireless station interface
    /// </summary>
    /// <param name="ssid"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static bool Configure(string ssid, string password)
    {
        // And we have to force connect once here even for a short time
        var success = WifiNetworkHelper.ConnectDhcp(ssid, password, token: new CancellationTokenSource(10000).Token);
        Debug.WriteLine($"Connection is {success}");
        var wconf = GetConfiguration();
        wconf.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect | Wireless80211Configuration.ConfigurationOptions.Enable;
        wconf.SaveConfiguration();
        return true;
    }

    /// <summary>
    /// Get the Wireless station configuration.
    /// </summary>
    /// <returns>Wireless80211Configuration object</returns>
    public static Wireless80211Configuration GetConfiguration()
    {
        var ni = Utilities.GetNetworkInterface();
        return Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];
    }
}
