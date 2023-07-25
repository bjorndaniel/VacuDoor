//FROM: https://github.com/nanoframework/Samples/tree/main/samples/WiFiAP
namespace VacuDoor;
internal class WirelessAP
{
    public const string SOFT_AP_IP = "192.168.4.1";

    /// <summary>
    /// Disable the Soft AP for next restart.
    /// </summary>
    public static void Disable()
    {
        var wapconf = GetConfiguration();
        wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.None;
        wapconf.SaveConfiguration();
    }

    /// <summary>
    /// Set-up the Wireless AP settings, enable and save
    /// </summary>
    /// <returns>True if already set-up</returns>
    public static bool Setup()
    {
        var ni = Utilities.GetNetworkInterface();
        var wapconf = GetConfiguration();
        // Set the SSID for Access Point. If not set will use default  "nano_xxxxxx"
        wapconf.Ssid = "VacuDoor";
        wapconf.SaveConfiguration();

        // Check if already Enabled and return true
        if (wapconf.Options ==
                (WirelessAPConfiguration.ConfigurationOptions.Enable | WirelessAPConfiguration.ConfigurationOptions.AutoStart)
                && ni.IPv4Address == SOFT_AP_IP
            )
        {
            return true;
        }

        // Set up IP address for Soft AP
        ni.EnableStaticIPv4(SOFT_AP_IP, "255.255.255.0", SOFT_AP_IP);

        // Set Options for Network Interface
        //
        // Enable    - Enable the Soft AP ( Disable to reduce power )
        // AutoStart - Start Soft AP when system boots.
        // HiddenSSID- Hide the SSID
        //
        wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.AutoStart |
                        WirelessAPConfiguration.ConfigurationOptions.Enable;



        // Maximum number of simultaneous connections, reserves memory for connections
        wapconf.MaxConnections = 1;

        // To set-up Access point with no Authentication
        wapconf.Authentication = AuthenticationType.Open;
        wapconf.Password = "";
        // Save the configuration so on restart Access point will be running.
        wapconf.SaveConfiguration();
        return false;
    }

    /// <summary>
    /// Find the Wireless AP configuration
    /// </summary>
    /// <returns>Wireless AP configuration or NUll if not available</returns>
    public static WirelessAPConfiguration GetConfiguration()
    {
        var ni = Utilities.GetNetworkInterface();
        return WirelessAPConfiguration.GetAllWirelessAPConfigurations()[ni.SpecificConfigId];
    }
}
