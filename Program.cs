using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SemkaHttpServer;
using MapAPI;
using CoordinateSharp;
using System.Globalization;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                Graph graph;
                using (BinaryReader bReader = new BinaryReader(new FileStream("./out4.srg", FileMode.Open)))
                    graph = Graph.Deserialize(bReader);

                DangerAddThread dangerAddThread = new DangerAddThread(graph);
                DangerSubThread dangerSubThread = new DangerSubThread(graph);
                new Thread(dangerAddThread.Run).Start();
                new Thread(dangerSubThread.Run).Start();
                HttpServer server = new HttpServer(11223);
                Console.WriteLine("Server is up");
                server.pathHandler["/"] = (HttpContext context) =>
                {
                    context.Log("The client has successfully passed to the main page");
                    context.ResponseHeaders["Content-Type"] = "text/html";
                    context.Response(200, "OK", File.ReadAllText("index.html"));
                };

                server.pathHandler["/randPath"] = (HttpContext context) =>
                {
                    context.Log("The client has requested random path");
                    context.Response(200, "OK", graph.RandTen().ToJsonString());
                };

                server.pathHandler["/AddDanger"] = (HttpContext context) =>
                {
                    double latO, longO, R;
                    int dangerType;

                    if (!context.QueryParameters.ContainsKey("latO") ||
                    !context.QueryParameters.ContainsKey("longO") ||
                    !context.QueryParameters.ContainsKey("r") ||
                    !context.QueryParameters.ContainsKey("dangertype"))
                    {
                        context.Response(400, "Bad Request", "Some parametrs are missing");
                        return;
                    }
                    if (!Double.TryParse(context.QueryParameters["latO"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out latO) ||
                    !Double.TryParse(context.QueryParameters["longO"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out longO) ||
                    !Double.TryParse(context.QueryParameters["r"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out R) ||
                    !int.TryParse(context.QueryParameters["dangertype"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dangerType))
                    {
                        context.Response(400, "Bad Request", "Some parametrs are wrong");
                        return;
                    }

                    Graph.NewDanger nd = new Graph.NewDanger(dangerType, latO, longO, R, graph);
                    dangerAddThread.ConQueue.Enqueue(nd);
                    context.Log("The client added new danger");
                    context.Response(200, "OK", "{ \"answer\" : \"OK\"}");
                };

                server.pathHandler["/FindPath"] = (HttpContext context) =>
                {
                    double latfrom, longfrom, latto, longto;

                    if (!context.QueryParameters.ContainsKey("latfrom") ||
                    !context.QueryParameters.ContainsKey("longfrom") ||
                    !context.QueryParameters.ContainsKey("latto") ||
                    !context.QueryParameters.ContainsKey("longto"))
                    {
                        context.Response(400, "Bad Request", "Some parametrs are missing");
                        return;
                    }
                    if (!Double.TryParse(context.QueryParameters["latfrom"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out latfrom) ||
                    !Double.TryParse(context.QueryParameters["longfrom"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out longfrom) ||
                    !Double.TryParse(context.QueryParameters["latto"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out latto) ||
                    !Double.TryParse(context.QueryParameters["longto"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out longto))
                    {
                        context.Response(400, "Bad Request", "Some parametrs are wrong");
                        return;
                    }

                    context.Log($"The client wants to find a path from [{latfrom}, {longfrom}] to [{latto}, {longto}]");
                    context.ResponseHeaders["Content-Type"] = "application/json";
                    Graph.Path? route = graph.FindPath(latfrom, longfrom, latto, longto);
                    if (route != null)
                        context.Response(200, "OK", ((Graph.Path)route).ToJsonString());
                    else
                        context.Response(200, "OK", "{ \"route\": \"No path was found\"}");
                };

                server.pathHandler["/licenses"] = (HttpContext context) =>
                {
                    context.Log("The client has successfully passed to the licenses' page");
                    context.ResponseHeaders["Content-Type"] = "text/html";
                    context.Response(200, "OK", File.ReadAllText("licenses.html"));
                };

                server.defaultHandler = (HttpContext context) =>
                {
                    if (File.Exists($".{context.requri}"))
                    {
                        string filetype = context.requri.Split("/").Last().Split(".").Last();

                        switch (filetype)
                        {
                            case "js":
                                {
                                    context.Log($"User was successfuly go on {context.requri}");
                                    context.ResponseHeaders["Content-Type"] = "text/javascript";
                                    context.Response(200, "OK", File.ReadAllText($".{context.requri}"));
                                    break;
                                }

                            case "css":
                                {
                                    context.Log($"User was succssefuly get css");
                                    context.ResponseHeaders["Content-Type"] = "text/css";
                                    context.Response(200, "OK", File.ReadAllText($".{context.requri}"));
                                    break;
                                }

                            case "svg":
                                {
                                    context.Log($"User was succssefuly get css");
                                    context.ResponseHeaders["Content-Type"] = "image/svg+xml";
                                    context.Response(200, "OK", File.ReadAllText($".{context.requri}"));
                                    break;
                                }
                            case "ico":
                                {
                                    context.ResponseHeaders["Content-Type"] = "image/x-icon";
                                    context.Response(200, "OK", File.ReadAllBytes($".{context.requri}"));
                                    break;
                                }

                            default:
                                {
                                    context.Log($"User wanted to go on {context.requri}");
                                    context.Response(404, "Not Found", "Something went wrong.");
                                    break;
                                }
                        }
                    }
                    else
                    {
                        context.Log($"User wanted to go on {context.requri}");
                        context.Response(404, "Not Found", "Something went wrong.");
                    }
                };

                server.Start();
                server.LogS("Waiting for shutdown");
                while (!File.Exists("./exit.txt"))
                {
                    Thread.Sleep(100);
                }

                if (File.Exists("./exit.txt"))
                {
                    File.Delete("./exit.txt");
                }
                server.LogS("Server was shut down");
                dangerAddThread.IsDisabled = true;
                dangerSubThread.IsDisabled = true;
                Thread.Sleep(1000);
                
            }
            catch (Exception e) 
            {
                File.AppendAllText("CrachLog", $" {DateTime.Now}: {e.Message} \n");
            }
        }
    }
}