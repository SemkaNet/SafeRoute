using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MapAPI
{
    public class DangerAddThread
    {
        private Graph graph;
        ConcurrentQueue<Graph.NewDanger> nds = new();
        private bool isDisabled = false;
        private string connectioninfo = "";

        public ConcurrentQueue<Graph.NewDanger> ConQueue => nds;
        public bool IsDisabled
        {
            set => isDisabled = value;
        }

        public DangerAddThread(Graph graph)
        {
            this.graph = graph;
            Console.WriteLine("DangerAddThread is up");
            using (NpgsqlConnection connection = new NpgsqlConnection(connectioninfo))
            {
                connection.Open();
                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"SELECT * FROM dangers";
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nds.Enqueue(new Graph.NewDanger((int)reader["type"], (double)reader["pointlat"], (double)reader["pointlong"], (double)reader["r"], graph));
                        }

                    }
                }
                connection.Close();
            }
        }

        public void Run()
        {
            Console.WriteLine("DangerAddThread is running");
            while (!isDisabled)
            {
                if (!nds.TryDequeue(out Graph.NewDanger nd))
                {
                    Thread.Sleep(100);
                    continue;
                }
                foreach (Node node in graph.Nodes)
                {
                    if (node.Distance(nd.point) <= nd.R)
                    {
                        lock (node)
                        {
                            switch (nd.dangerType)
                            {
                                case 1:
                                    node.Light += 80;
                                    break;
                                case 2:
                                    node.Precinct += 70;
                                    break;
                                case 3:
                                    node.Dogs += 50;
                                    break;
                                case 4:
                                    node.Wilddogs += 100;
                                    break;
                                case 5:
                                    node.Crime += 80;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                using (NpgsqlConnection connection = new NpgsqlConnection(connectioninfo))
                {
                    connection.Open();
                    using (NpgsqlCommand command = connection.CreateCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = $"INSERT INTO dangers(type, pointlat, pointlong, r, enddate) " +
                            $"VALUES({nd.dangerType}, {nd.point.Latitude}, {nd.point.Longitude}, {nd.R}, '{DateTime.Now.AddMinutes(5).ToString("s")}')";
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
        }
    }

    public class DangerSubThread
    {
        private string connectioninfo = "";
        private bool isDisabled = false;
        private Graph graph;

        public bool IsDisabled
        {
            set => isDisabled = value;
        }

        public DangerSubThread(Graph graph)
        {
            this.graph = graph;
            Console.WriteLine("DangerSubThread is up");
        }

        public void Run()
        {
            Console.WriteLine("DangerSubThread is running");
            using (NpgsqlConnection connection = new NpgsqlConnection(connectioninfo))
            {
                connection.Open();
                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    
                    command.Connection = connection;

                    while (!isDisabled)
                    {
                        command.CommandText = $"SELECT * FROM dangers ORDER BY enddate LIMIT 1";
                        int id;
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            Graph.NewDanger nd;
                            DateTime enddate;
                            if (!reader.Read())
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            nd = new Graph.NewDanger((int)reader["type"], (double)reader["pointlat"], (double)reader["pointlong"], (double)reader["r"], graph);
                            enddate = (DateTime)reader["enddate"];
                            id = (int)reader["id"];
                            if (DateTime.Now < enddate)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            foreach (Node node in graph.Nodes)
                            {
                                if (node.Distance(nd.point) <= nd.R)
                                {
                                    lock (node)
                                    {
                                        switch (nd.dangerType)
                                        {
                                            case 1:
                                                node.Light -= 80;
                                                break;
                                            case 2:
                                                node.Precinct -= 70;
                                                break;
                                            case 3:
                                                node.Dogs -= 50;
                                                break;
                                            case 4:
                                                node.Wilddogs -= 100;
                                                break;
                                            case 5:
                                                node.Crime -= 80;
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        command.CommandText = $"DELETE FROM dangers WHERE id = {id}";
                        command.ExecuteNonQuery();
                    }    
                }
                connection.Close();
            }
        }
    }
}
