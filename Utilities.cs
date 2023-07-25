namespace VacuDoor;

internal class Utilities
{
    public static NetworkInterface GetNetworkInterface()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();

        // Find WirelessAP interface
        foreach (var ni in interfaces)
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.WirelessAP)
            {
                return ni;
            }
        }
        throw new ArgumentException("No WirelessAP interface found");
    }

    /// <summary>
    /// Returns the IP address of the Soft AP
    /// </summary>
    /// <returns>IP address</returns>
    public static string GetIP()
    {
        var ni = GetNetworkInterface();
        return ni.IPv4Address;
    }
}
