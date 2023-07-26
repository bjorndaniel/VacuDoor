//WIFI SETUP FROM: https://github.com/nanoframework/Samples/tree/main/samples/WiFiAP

namespace VacuDoor;
public class Program
{
    // Connected Station count
    private static int _connectedCount = 0;
    // GPIO pin used to put device into AP set-up mode
    private const int SETUP_PIN = 5;
    //Period of the PWM signal in milliseconds
    private const double PERIOD = 10.0;
    //Web server to use for AP set-up
    private static ApWebServer _apServer = new();
    private static ControlWebServer _controlServer;
    private static ServoMotor _servoMotor;

    public static void Main()
    {
        Debug.WriteLine("VacuDoor starting!");
        InitializeWifi();
        _apServer.Stop();
        Configuration.SetPinFunction(16, DeviceFunction.PWM1);

        using PwmChannel pwmChannel = PwmChannel.CreateFromPin(16, 50);
        _servoMotor = new ServoMotor(
            pwmChannel,
            180,
            544,
            2400);
        _servoMotor.Start();
        _controlServer = new();
        _controlServer.Start(_servoMotor);
        Thread.Sleep(Timeout.Infinite);
    }

    private static void InitializeWifi()
    {
        var gpioController = new GpioController();
        var setupButton = gpioController.OpenPin(SETUP_PIN, PinMode.InputPullDown);
        if (!Wireless80211.IsEnabled() || setupButton.Read() == PinValue.Low)
        {
            Wireless80211.Disable();
            if (WirelessAP.Setup() == false)
            {
                // Reboot device to Activate Access Point on restart
                Debug.WriteLine($"Setup Soft AP, Rebooting device");
                Power.RebootDevice();
            }

            var dhcpserver = new DhcpServer
            {
                CaptivePortalUrl = $"http://{WirelessAP.SOFT_AP_IP}"
            };
            var dhcpInitResult = dhcpserver.Start(IPAddress.Parse(WirelessAP.SOFT_AP_IP), new IPAddress(new byte[] { 255, 255, 255, 0 }));
            if (!dhcpInitResult)
            {
                Debug.WriteLine($"Error initializing DHCP server.");
            }

            Debug.WriteLine($"Running Soft AP, waiting for client to connect");
            Debug.WriteLine($"Soft AP IP address :{Utilities.GetIP()}");

            // Link up Network event to show Stations connecting/disconnecting to Access point.
            //NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;
            // Now that the normal Wifi is deactivated, that we have setup a static IP
            // We can start the Web server
            //_apServer.Start();
        }
        else
        {
            Debug.WriteLine($"Running in normal mode, connecting to Access point");
            var conf = Wireless80211.GetConfiguration();
            bool success;

            // For devices like STM32, the password can't be read
            if (string.IsNullOrEmpty(conf.Password))
            {
                // In this case, we will let the automatic connection happen
                success = WifiNetworkHelper.Reconnect(requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
            }
            else
            {
                // If we have access to the password, we will force the reconnection
                // This is mainly for ESP32 which will connect normaly like that.
                success = WifiNetworkHelper.ConnectDhcp(conf.Ssid, conf.Password, requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
            }

            if (success)
            {
                Debug.WriteLine($"Connection is {success}");
                Debug.WriteLine($"We have a valid date: {DateTime.UtcNow}");
            }
            else
            {
                Debug.WriteLine($"Something wrong happened, can't connect at all");
            }
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                Debug.WriteLine($"Ip Address: {ni.IPv4Address}");
            }
        }
    }
}