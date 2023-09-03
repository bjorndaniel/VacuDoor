//WIFI SETUP FROM: https://github.com/nanoframework/Samples/tree/main/samples/WiFiAP

namespace VacuDoor;
public class Program
{
    // Connected Station count
    private static int _connectedCount = 0;
    // GPIO pin used to put device into AP set-up mode
    private const int SETUP_PIN = 5;
    private const int LASER_PIN = 23;
    private const int LASER_TARGET_PIN = 19;
    //Period of the PWM signal in milliseconds
    private const double PERIOD = 10.0;
    //Web server to use for AP set-up
    private static ApWebServer _apServer = new();
    private static ControlWebServer? _controlServer;
    private static ServoMotor? _servoMotor;
    private static Ssd1306? _oled;
    private static GpioController _gpioController = new();
    private static GpioPin? _laserPin;
    private static GpioPin? _laserTargetPin;

    public static void Main()
    {
        Debug.WriteLine("VacuDoor starting!");
        _laserPin = _gpioController.OpenPin(LASER_PIN, PinMode.Output);
        _laserTargetPin = _gpioController.OpenPin(LASER_TARGET_PIN, PinMode.Input);
        _laserTargetPin.ValueChanged += OnLaserTargetChanged; ;
        _laserPin!.Write(PinValue.High);
        Debug.WriteLine("OLED Init");
        int dataPin = 22;
        int clockPin = 21;
        try
        {
            Configuration.SetPinFunction(dataPin, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(clockPin, DeviceFunction.I2C1_CLOCK);
            var i2c = I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress));
            _oled = new Ssd1306(i2c, Ssd13xx.DisplayResolution.OLED128x64);
            var clkpin = Configuration.GetFunctionPin(DeviceFunction.I2C1_CLOCK);
            var datpin = Configuration.GetFunctionPin(DeviceFunction.I2C1_DATA);
            Debug.WriteLine("Clock Pin " + clkpin + " DataPin: " + dataPin);
            _oled.Font = new BasicFont();
            Debug.WriteLine("OLED Clear Screen");
            _oled.ClearScreen();
            Debug.WriteLine("OLED Write Screen");
            _oled.DrawString(2, 16, "Initializing...", 1, true);
            _oled.Display();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error: " + ex.Message);
        }


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
        _controlServer.Start(ref _servoMotor, ref _laserPin);

        Thread.Sleep(Timeout.Infinite);
    }

    private static void OnLaserTargetChanged(object sender, PinValueChangedEventArgs e)
    {
        var value = _laserTargetPin!.Read();
        var laserOn = _laserPin!.Read() == PinValue.Low;
        if (value == PinValue.Low && laserOn)
        {
            ServoControl.Close(ref _servoMotor);
        }
        //Debug.WriteLine($"Laser Target Changed {value}");
        Debug.WriteLine($"Laser Target Changed {DateTime.UtcNow.Ticks}");
    }

    private static void InitializeWifi()
    {
        var setupButton = _gpioController.OpenPin(SETUP_PIN, PinMode.InputPullUp);
        var pinValue = setupButton.Read();

        if (!Wireless80211.IsEnabled() || pinValue == PinValue.High)
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
            _oled!.ClearScreen();
            Debug.WriteLine("OLED Write Screen");
            _oled!.DrawString(0, 10, "Connect to:", 1, true);
            _oled!.DrawString(0, 26, Utilities.GetIP(), 1, true);
            _oled!.Display();
            // Link up Network event to show Stations connecting/disconnecting to Access point.
            //NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;
            // Now that the normal Wifi is deactivated, that we have setup a static IP
            // We can start the Web server
            _apServer.Start();
            Thread.Sleep(Timeout.Infinite);
        }
        else
        {
            Debug.WriteLine($"Running in normal mode, connecting to Access point");
            var conf = Wireless80211.GetConfiguration();
            bool success;
            _oled!.ClearScreen();
            _oled!.DrawString(0, 10, "Connecting to:", 1, true);
            _oled!.DrawString(0, 26, conf.Ssid, 1, true);
            _oled!.Display();
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
                success = WifiNetworkHelper.ConnectDhcp(conf.Ssid, conf.Password, requiresDateTime: true, token: new CancellationTokenSource(90000).Token);
            }
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                Debug.WriteLine($"Ip Address: {ni.IPv4Address}");
            }
            if (success)
            {
                Debug.WriteLine($"Connection is {success}");
                Debug.WriteLine($"We have a valid date: {DateTime.UtcNow}");
                _oled!.ClearScreen();
                _oled!.DrawString(0, 10, "Connected to:", 1, true);
                _oled!.DrawString(0, 26, conf.Ssid, 1, true);
                _oled!.DrawString(0, 42, networkInterfaces[0].IPv4Address, 1, true);
                _oled!.Display();
                _laserPin!.Write(PinValue.Low);
            }
            else
            {
                _oled!.ClearScreen();
                _oled!.DrawString(0, 10, "Could not connect:", 1, true);
                _oled!.DrawString(0, 26, conf.Ssid, 1, true);
                _oled!.DrawString(0, 42, "Retry in AP mode", 1, true);
                _oled!.Display();
                Debug.WriteLine($"Something wrong happened, can't connect at all");
            }

        }
    }
}