namespace VacuDoor;

internal class ControlWebServer
{
    private HttpListener? _listener;
    private Thread? _serverThread;
    private static ServoMotor _servoMotor;

    public void Start(ServoMotor servoMotor)
    {
        if (_listener == null)
        {
            _listener = new HttpListener("http");
            _serverThread = new Thread(RunServer);
            _serverThread.Start();
            _servoMotor = servoMotor;
        }
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Stop();
        }
    }

    private void RunServer()
    {
        _listener!.Start();

        while (_listener.IsListening)
        {
            var context = _listener.GetContext();
            if (context != null)
            {
                ProcessRequest(context);
            }
        }
        _listener.Close();

        _listener = null;
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var responseString = string.Empty;
        var ssid = string.Empty;
        var password = string.Empty;
        var isApSet = false;

        switch (request.HttpMethod)
        {
            case "GET":
                var url = request.RawUrl.Split('?');
                if (url[0] == "/favicon.ico")
                {
                    response.ContentType = "image/png";
                    var responseBytes = Resources.GetBytes(Resources.BinaryResources.favicon);
                    OutPutByteResponse(response, responseBytes);
                }
                else if (!url[0].Contains("api"))
                {
                    response.ContentType = "text/html";
                    responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.mainControl), "");
                    OutPutResponse(response, responseString);
                }
                else if (url[0].Contains("open"))
                {
                    var result = ServoControl.Open(_servoMotor);

                    OutPutResponse(response, $"{{ \"status\": \"{(result ? "ok" : "error")}\" }}");
                }
                else if (url[0].Contains("close"))
                {
                    var result = ServoControl.Close(_servoMotor);
                    OutPutResponse(response, $"{{ \"status\": \"{(result ? "ok" : "error")}\" }}");
                }
                break;
        }

        response.Close();

        if (isApSet && (!string.IsNullOrEmpty(ssid)) && (!string.IsNullOrEmpty(password)))
        {
            // Enable the Wireless station interface
            Wireless80211.Configure(ssid, password);

            // Disable the Soft AP
            WirelessAP.Disable();
            Thread.Sleep(200);
            Power.RebootDevice();
        }
    }

    static string ReplaceMessage(string page, string message)
    {
        int index = page.IndexOf("{message}");
        if (index >= 0)
        {
            return page.Substring(0, index) + message + page.Substring(index + 9);
        }

        return page;
    }

    static void OutPutResponse(HttpListenerResponse response, string responseString) =>
        OutPutByteResponse(response, System.Text.Encoding.UTF8.GetBytes(responseString));

    static void OutPutByteResponse(HttpListenerResponse response, byte[] responseBytes)
    {
        response.ContentLength64 = responseBytes.Length;
        response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
    }

    static Hashtable ParseParamsFromStream(Stream inputStream)
    {
        var buffer = new byte[inputStream.Length];
        inputStream.Read(buffer, 0, (int)inputStream.Length);

        return ParseParams(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length));
    }

    static Hashtable ParseParams(string rawParams)
    {
        var hash = new Hashtable();

        var parPairs = rawParams.Split('&');
        foreach (string pair in parPairs)
        {
            var nameValue = pair.Split('=');
            hash.Add(nameValue[0], nameValue[1]);
        }
        return hash;
    }
}
