//WIFI SETUP FROM: https://github.com/nanoframework/Samples/tree/main/samples/WiFiAP

namespace VacuDoor;
public class Program
{
    // Connected Station count
    private static int connectedCount = 0;
    // GPIO pin used to put device into AP set-up mode
    private const int SETUP_PIN = 5;
    //Period of the PWM signal in milliseconds
    private const double PERIOD = 10.0;
    //Web server to use for AP set-up
    private static ApWebServer apServer = new();

    public static void Main()
    {
        Debug.WriteLine("VacuDoor starting!");
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
            apServer.Start();
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
        // Just wait for now
        // Here you would have the reset of your program using the client WiFI link
        Thread.Sleep(Timeout.Infinite);
    }
}



//Configuration.SetPinFunction(16, DeviceFunction.PWM1);

//using PwmChannel pwmChannel = PwmChannel.CreateFromPin(16, 50);
//var servoMotor = new ServoMotor(
//    pwmChannel,
//    180,
//    900,
//    2100);

//servoMotor.Start();  // Enable control signal.

//// Move position.
//servoMotor.WriteAngle(0); // ~0.9ms; Approximately 0 degrees.
//Thread.Sleep(1500);
////servoMotor.WriteAngle(90); // ~0.9ms; Approximately 0 degrees.
//Thread.Sleep(1500);
//servoMotor.WriteAngle(0); // ~0.9ms; Approximately 0 degrees.

//servoMotor.WritePulseWidth(90); // ~1.5ms; Approximately 90 degrees.
//servoMotor.WritePulseWidth(180); // ~2.1ms; Approximately 180 degrees.

//servoMotor.Stop(); // Disable control signal.
//Configuration.SetPinFunction(22, DeviceFunction.PWM2);
//var sw = Stopwatch.StartNew();
//// 1 pin mode
////using (DCMotor motor = DCMotor.Create(16))
////using (DCMotor motor = DCMotor.Create(PwmChannel.Create(9, 0, frequency: 50)))
//// 2 pin mode
//using (DCMotor motor = DCMotor.Create(16, 22))
////using (DCMotor motor = DCMotor.Create(new SoftwarePwmChannel(16, frequency: 50), 22))
//// 2 pin mode with BiDirectional Pin
//// using (DCMotor motor = DCMotor.Create(19, 26, null, true, true))
//// using (DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 1, 100, 0.0), 26, null, true, true))
//// 3 pin mode
//// using (DCMotor motor = DCMotor.Create(PwmChannel.Create(0, 0, frequency: 50), 23, 24))
//// Start Stop mode - wrapper with additional methods to disable/enable output regardless of the Speed value
//// using (DCMotorWithStartStop motor = new DCMotorWithStartStop(DCMotor.Create( _any version above_ )))
////using (DCMotor motor = DCMotor.Create(6, 27, 22))
//{
//    bool done = false;
//    motor.Speed = 0.0;
//    Thread.Sleep(2000);
//    Debug.WriteLine("Starting");
//    string lastSpeedDisp = null;
//    while (!done)
//    {
//        double time = sw.ElapsedMilliseconds / 1000.0;

//        // Note: range is from -1 .. 1 (for 1 pin setup 0 .. 1)
//        motor.Speed = 0.05; //Math.Sin(2.0 * Math.PI * 1 / Period);
//        var disp = $"Speed = {motor.Speed:0.00}";
//        //if (disp != lastSpeedDisp)
//        //{
//        //    lastSpeedDisp = disp;
//        Debug.WriteLine(disp);
//        //}

//        Thread.Sleep(1000);
//        Debug.WriteLine("Reversing");
//        motor.Speed = -0.00;
//        motor.Speed = -0.05;
//        Thread.Sleep(1000);
//        Debug.WriteLine("Stopping");
//        motor.Speed = 0;
//        done = true;
//    }
//}


////var sw = Stopwatch.StartNew();
////Thread.Sleep(Timeout.Infinite);

//// Browse our samples repository: https://github.com/nanoframework/samples
//// Check our documentation online: https://docs.nanoframework.net/
//// Join our lively Discord community: https://discord.gg/gCyBu8T