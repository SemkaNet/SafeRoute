using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;
using System.Web;

namespace SemkaHttpServer
{
    public class HttpContext : IDisposable
    {
        public string reqmethod, requri, clientstr;
        NetworkStream stream;
        FormatException? _e2 = null;
        StreamWriter sw;
        TcpClient _client;
        Dictionary<string, string> _requestHeaders;
        Dictionary<string, string> _responseHeaders;
        Dictionary<string, string> _queryParameters;

        public Dictionary<string, string> RequestHeaders
        {
            get => _requestHeaders;
        }
        public Dictionary<string, string> ResponseHeaders
        {
            get => _responseHeaders;
        }
        public Dictionary<string, string> QueryParameters
        {
            get => _queryParameters;
        }

        public NetworkStream Stream
        {
            get => stream;
        }
        public HttpContext(TcpClient client)
        {
            _client = client;
            stream = client.GetStream();
            sw = new StreamWriter(stream, null, -1, true);
        }

        public void Log(string message)
        {
            lock (HttpServer._lock)
            {
                File.AppendAllText("log.txt", $"\n{DateTime.Now}: {message}. IP {((IPEndPoint)_client.Client.RemoteEndPoint).Address}:{((IPEndPoint)_client.Client.RemoteEndPoint).Port}");
            }
        }

        public void Dispose()
        {
            stream.Flush();
            Thread.Sleep(10000);
            sw.Dispose();
            _client.Close();
            _client.Dispose();
        }

        public void ReadBuffer(byte[] buffer, int timeout = -1)
        {
            int wait = 0;
            int overall = 0;
            do
            {
                Thread.Sleep(1);
                wait += 1;
                if ((timeout >= 0) && (timeout < wait))
                    throw new TimeoutException("Read timeout");
                overall += stream.Read(buffer, overall, buffer.Length - overall);
            }
            while (overall < buffer.Length);
            
        }

        public string RLS() // Read Line from the Stream
        {
            string result = "";
            char memr;
            while ((memr = Convert.ToChar(stream.ReadByte())) != '\r')
            {
                result += memr;
            }
            stream.ReadByte();
            return result;
        }

        public void WLS(string text) // Write Line into the Stream
        {
            sw.WriteLine(text);
            sw.Flush();
        }

        public void WS(string text) // Write into the Stream
        {
            sw.Write(text);
            sw.Flush();
        }

        public void WB(Byte[] buffer)
        {
            stream.Write(buffer);
            stream.Flush();
        }

        private string BaseSpl(string basetext) // Split phrase from word Basic
        {
            return (basetext.Split(' '))[1] ?? "гыг";
        }

        public void Response(int statcode, string statdescription, Byte[] content)
        {
            WLS($"HTTP/1.1 {statcode} {statdescription}");
            WLS($"Content-Length: {content.Length}");
            WLS($"Connection: close");
            if (_responseHeaders.ContainsKey("Content-Type"))
                _responseHeaders["Content-Type"] += "; charset=utf-8";
            foreach (KeyValuePair<string, string> headers in _responseHeaders)
            {
                WLS($"{headers.Key}: {headers.Value}");
            }
            WLS("");
            WB(content);
        }

        public void Response(int statcode, string statdescription, string content)
        {
            Byte[] buffer = Encoding.UTF8.GetBytes(content);
            Response(statcode, statdescription, buffer);
        }

        public Dictionary<string, string> HeadersRecognise()
        {
            string[] request = new string[1];
            Dictionary<string, string> headers = new();
            while ((clientstr = RLS()) != "")
            {
                request = clientstr.Split(": ");
                headers.Add(request[0], request[1]);
            }
            return headers;
        }

        public void QueryParametersRecognise(string queryParameters)
        {
            foreach (string parameters in queryParameters.Split('&'))
            {
                string[] parameter = parameters.Split('=');
                _queryParameters.Add(HttpUtility.UrlDecode(parameter[0]), HttpUtility.UrlDecode(parameter[1]));
            }
        }

        public void Run()
        {
            string[] request = new string[1];
            string[] uriQuery;
            double httpver;
            _requestHeaders = new();
            _responseHeaders = new();
            _queryParameters = new();
            try
            {
                request[0] = RLS();
                request = request[0].Split(' ');
                reqmethod = request[0];
                uriQuery = request[1].Split('?');
                requri = String.Join('/', uriQuery[0].Split('/').Where(w => w != "." && w != ".."));
                if (uriQuery.Length > 1)
                    QueryParametersRecognise(uriQuery[1]);
                httpver = Convert.ToDouble((request[2].Split('/'))[1], System.Globalization.CultureInfo.InvariantCulture);

                if (httpver < 1)
                {
                    Log("Can't support HTTP version");
                    sw.WriteLine("We can't support your HTTP version");
                }

                _requestHeaders = HeadersRecognise();

                //End of reading client's request
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.StackTrace);
            }
        }

        
    }

    public class HttpServer
    {
        public delegate void Context (HttpContext context);
        public Dictionary<string, Context> pathHandler = new();
        private TcpListener _server = null;
        public static object _lock = new();
        int port;

        public Context defaultHandler = null;
        public HttpServer(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            _server = new TcpListener(port);
            _server.Start();
            new Task(Listen).Start();
        }

        public void LogS(string message)
        {
            File.AppendAllText("ServerLog.txt", $". {DateTime.Now}: {message} \n");
        }

        public void Listen()
        {
            while (true)
            {
                try
                {
                    LogS("The server is up");
                    TcpClient client = _server.AcceptTcpClient();

                    new Task(() => {
                        try
                        {
                            using (HttpContext context = new HttpContext(client))
                            {
                                context.Run();
                                context.Log("Client was connected");
                                if (pathHandler.ContainsKey(context.requri))
                                    pathHandler[context.requri](context);
                                else if (defaultHandler == null)
                                    context.Response(404, "Not Found", "Something went wrong.");
                                else
                                    defaultHandler(context);
                            }
                        }
                        catch (Exception e)
                        {
                            LogS(e.Message);
                        }
                    }).Start();
                }
                catch (Exception e)
                {
                    LogS(e.Message);
                }
            }
        }
    }

}
