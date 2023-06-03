using CoordinateSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace MapAPI
{
    public class Node
    {
        // Field
        private Int64 id;
        private double weight, light, precinct, dogs, wilddogs, crime; // weight is a length of Node in meters, rest part is a dangers weigths
        private double latitude, longitude;
        private List<Node> neighbors = new List<Node>();
        private Dictionary<Int64, Node> neighborsDictionary = new Dictionary<Int64, Node>();
        static Celestial cel = Celestial.CalculateCelestialTimes(55.04570045, 82.91398145000001, DateTime.Now);
        // Properties
        public Int64 Id => id;
        public double Weight => weight;
        public double Light
        {
            get => light;
            set => light = value;
        }
        public double Precinct
        {
            get => precinct;
            set => precinct = value;
        }
        public double Dogs
        {
            get => dogs;
            set => dogs = value;
        }
        public double Wilddogs
        {
            get => wilddogs;
            set => wilddogs = value;
        }
        public double Crime
        {
            get => crime;
            set => crime = value;
        }
        public double Danger => light * ((cel.IsSunUp) ? 0 : 1) + precinct + dogs + wilddogs + crime; 
        public double Latitude => latitude;
        public double Longitude => longitude;
        public List<Node> Neighbors => neighbors;
        public Dictionary<Int64, Node> NeighborsDictionary => neighborsDictionary;

        // Constructor
        public Node(Int64 id, double weight, double longitude, double latitude, double light = 0, double precinct = 0, double dogs = 0, double wilddogs = 0, double crime = 0)
        {
            this.id = id;
            this.weight = weight;
            this.latitude = latitude;
            this.longitude = longitude;
            this.light = light;
            this.precinct = precinct;
            this.dogs = dogs;
            this.wilddogs = wilddogs;
            this.crime = crime;
        }
        // Methods
        public void AddNeighbor(Node neighbor)
        {
            if (neighbor != this)
            {
                if (!neighborsDictionary.ContainsKey(neighbor.Id))
                {
                    neighborsDictionary[neighbor.Id] = neighbor;
                    neighbors.Add(neighbor);
                }
                if (!neighbor.NeighborsDictionary.ContainsKey(this.id))
                {
                    neighbor.NeighborsDictionary[this.id] = this;
                    neighbor.Neighbors.Add(this);
                }
            }
        }
        
        public void DeleteNeighbor(Node neighbor)
        {
            neighbors.Remove(neighbor);
        }

        public override string ToString()
        {
            return $"Name: {id}, Longitude: {longitude}, Latitude: {latitude}";
        }

        public double Distance(Node neighbor)
        {
            double R = 6371008.7714;
            double x = ((Math.PI / 180) * neighbor.Longitude - (Math.PI / 180) * longitude) * Math.Cos(0.5 * ((Math.PI / 180) * neighbor.Latitude + (Math.PI / 180) * latitude));
            double y = ((Math.PI / 180) * neighbor.Latitude) - ((Math.PI / 180) * latitude);
            return R * Math.Sqrt(x * x + y * y);
        }

        public void Serialize(BinaryWriter bWriter)
        {
            // Information about this Node
            bWriter.Write(id);
            bWriter.Write(weight);
            bWriter.Write(longitude);
            bWriter.Write(latitude);
            bWriter.Write(light);
            bWriter.Write(precinct);
            bWriter.Write(dogs);
            bWriter.Write(wilddogs);
            bWriter.Write(crime);
            // Information about neighbors Nodes
            bWriter.Write(neighbors.Count);

            foreach (Node neighbor in neighbors) 
            {
                bWriter.Write(neighbor.Id);
            }
        }

        public static Node Deserialize(BinaryReader bReader, Graph graph)
        {
            // Create new Node
            Node node = new Node(bReader.ReadInt64(), bReader.ReadDouble(),
                bReader.ReadDouble(), bReader.ReadDouble(), bReader.ReadDouble(),
                bReader.ReadDouble(), bReader.ReadDouble(), bReader.ReadDouble(),
                bReader.ReadDouble());
            // Read Neighbors
            int num = bReader.ReadInt32();
            
            for (int i = 0; i < num; i += 1) 
            {
                Node neighbor;
                if ((neighbor = graph.GetById(bReader.ReadInt64())) != null)
                    node.AddNeighbor(neighbor);
            }
            return node;
        }

        public string ToJsonString()
        {
            return $"{{\"Danger\": {this.Danger}, \"Longitude\": {this.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, \"Latitude\": {this.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
        }
    }
    public class Graph //Class describes node of graphs
    {
        List<Node> nodes = new List<Node>();
        Dictionary<Int64, Node> nodesDictionary = new();
		public List<Node> Nodes => nodes;
		
        public void AddNode(Node node)
        {
            if (!nodesDictionary.ContainsKey(node.Id))
            {
                nodes.Add(node);
                nodesDictionary[node.Id] = node;
            }
        }
        
        public Node? GetById(Int64 id)
        {
            return nodesDictionary.TryGetValue(id, out Node node) ? node : null ;
        }
        public void DescribeNode(Int64 id)
        {
            Node? node = GetById(id);
            if (node != null)
            {
                Console.WriteLine($"Node \"{id}\":\nWeight = {node.Weight}\nLatitude = {node.Latitude}\nLongitude = {node.Longitude}");
                foreach (Node neighbor in node.Neighbors)
                {
                    Console.WriteLine($"Neighbor = {neighbor.Id}");
                }
            }
            else
                Console.WriteLine("No such Node");
        }

        public void Serialize(BinaryWriter bWriter)
        {
            // Write number of nodes in Graph
            bWriter.Write(nodes.Count);
            
            foreach (Node node in nodes) 
            {
                node.Serialize(bWriter);
            }
        }

        public static Graph Deserialize(BinaryReader bReader)
        {
            Graph graph = new Graph();
            // Read numbers of nodes in Graph
            int num = bReader.ReadInt32();
            for (int i = 0; i < num; i += 1) 
            {
                graph.AddNode(Node.Deserialize(bReader, graph));
            }

            return graph;
        }

        public Node NearbyNode(double latitude, double longitude)
        {
            Node precise = new Node(11, 10, longitude, latitude);
            Node? nearby = null;
            double dist = -1;
            double newdist;

            for (int i = 0; i < nodes.Count; i += 1)
            {
                newdist = precise.Distance(nodes[i]);
                if (nearby == null || newdist < dist)
                {
                    dist = newdist;
                    nearby = nodes[i];
                }
            }
            return nearby;
        }

        public class treeNodeInfo : IComparable<treeNodeInfo>
        {
            public double length;
            public Node parent, self;
            public bool visited;
            public double timeStamp;

            public treeNodeInfo(double length, Node parent, Node self, double timeStamp)
            {
                this.length = length;
                this.parent = parent;
                this.self = self;
                visited = false;
                this.timeStamp = timeStamp;
            }
            public int CompareTo(treeNodeInfo? other) => (this > other) ? 1 : (this < other) ? -1 : 0;
            public static bool operator <(treeNodeInfo a, treeNodeInfo b) => ((a.length < b.length) || ((a.length == b.length) && (a.timeStamp < b.timeStamp)));
            public static bool operator >(treeNodeInfo a, treeNodeInfo b) => ((a.length > b.length) || ((a.length == b.length) && (a.timeStamp > b.timeStamp)));
            public static bool operator ==(treeNodeInfo a, treeNodeInfo b) => (a.length == b.length) && (a.timeStamp == b.timeStamp);
            public static bool operator !=(treeNodeInfo a, treeNodeInfo b) => (a.length != b.length) || (a.timeStamp != b.timeStamp);
            public static bool operator >=(treeNodeInfo a, treeNodeInfo b) => (a > b) || (a == b);
            public static bool operator <=(treeNodeInfo a, treeNodeInfo b) => (a < b) || (a == b);
        }

        public struct Path
        {
            public Node[] pathNodes;
            Dictionary<Node, NodeInfo>? tree = null;
            public double pathLength = 0;
            public Path (List<Node> nodes)
            {
                pathNodes = nodes.ToArray();
                for (int i = 0; i < pathNodes.Length - 1; i += 1)
                {
                    pathLength += pathNodes[i].Distance(pathNodes[i + 1]);
                }
            }

            public string ToJsonString() // [{"Id": 1, "Longitude": 1, "Latitude": 1},...]
            {
                string result = "[";
                
                foreach (Node node in pathNodes)
                {
                    result += ((result == "[") ? "" : ", ") + node.ToJsonString();
                }
                return result + "]";
            }
        }

        public Path Dijkstra(Node start, Node finish)
        {
            List<Node> path = new();
            Node tmp = start;
            Dictionary<Node, treeNodeInfo> tree = new();
            double count = Math.Pow(1, -9);
            treeNodeInfo tmpInfo = new(0, null, tmp, count);
            tmpInfo.length = 0;
            tmpInfo.parent = null;
            tmpInfo.visited = true;
            tree.Add(tmp, tmpInfo);
            SortedSet<treeNodeInfo> set = new();

            while (tmp != finish)
            {
                foreach (Node neighbor in tmp.Neighbors)
                {
                    if (!tree.ContainsKey(neighbor))
                    {
                        count += Math.Pow(1, -9);
                        treeNodeInfo neighborInfo = new treeNodeInfo((tmp.Weight + tree[tmp].length), tmp, neighbor, count);
                        tree.Add(neighbor, neighborInfo);
                        set.Add(neighborInfo);
                    }
                    else 
                    {
                        treeNodeInfo neighborInfo = tree[neighbor];
                        if (!neighborInfo.visited)
                        {
                            if (neighborInfo.length > (tmp.Weight + tree[tmp].length))
                            {
                                set.Remove(neighborInfo);
                                neighborInfo.length = tmp.Weight + tree[tmp].length;
                                neighborInfo.parent = tmp;
                                set.Add(neighborInfo);
                            }
                        }
                    }
                }

                if (set.Count == 0)
                {
                    Console.WriteLine("There is no path");
                    throw new Exception("There is no path");
                }

                treeNodeInfo NewtmpInfo = set.Min;
                set.Remove(NewtmpInfo);
                tmp = NewtmpInfo.self;
                NewtmpInfo.visited = true;
            }

            do
            {
                path.Add(tmp);
                tmp = tree[tmp].parent;
            } 
            while (tmp != null);
            path.Reverse();
            Path fullPath = new Path(path);
            return fullPath;
        }

        public class NodeInfo : IComparable<NodeInfo>
        {
            double fullcost, danger, distanceToEnd, length;
            public Node parent, self;
            public bool visited;
            public double timeStamp;

            public double Fullcost => fullcost;
            public double Length
            {
                get => length;
                set 
                {
                    length = value;
                    fullcost = length + distanceToEnd + danger;
                }
            }

            public NodeInfo(double length, Node finish, Node parent, Node self, double timeStamp, Random random)
            {
                this.length = length;
                distanceToEnd = self.Distance(finish);
                danger = self.Danger + random.NextDouble() * 2;
                fullcost = this.length + distanceToEnd + danger;
                this.parent = parent;
                this.self = self;
                visited = false;
                this.timeStamp = timeStamp;
            }

            public int CompareTo(NodeInfo? other) => (this > other) ? 1 : (this < other) ? -1 : 0;
            public static bool operator <(NodeInfo a, NodeInfo b) => ((a.fullcost < b.fullcost) || ((a.fullcost == b.fullcost) && (a.timeStamp < b.timeStamp)));
            public static bool operator >(NodeInfo a, NodeInfo b) => ((a.fullcost > b.fullcost) || ((a.fullcost == b.fullcost) && (a.timeStamp > b.timeStamp)));
            public static bool operator ==(NodeInfo a, NodeInfo b) => (a.fullcost == b.fullcost) && (a.timeStamp == b.timeStamp);
            public static bool operator !=(NodeInfo a, NodeInfo b) => (a.fullcost != b.fullcost) || (a.timeStamp != b.timeStamp);
            public static bool operator >=(NodeInfo a, NodeInfo b) => (a > b) || (a == b);
            public static bool operator <=(NodeInfo a, NodeInfo b) => (a < b) || (a == b);
        }

        public Path? AStar(Node start, Node finish)
        {
            List<Node> path = new();
            Node tmp = start;
            Dictionary<Node, NodeInfo> tree = new();
            double count = Math.Pow(1, -9);
            Random random = new Random();
            NodeInfo tmpInfo = new(0, finish, null, tmp, count, random);
            tmpInfo.Length = 0;
            tmpInfo.parent = null;
            tmpInfo.visited = true;
            tree.Add(tmp, tmpInfo);
            SortedSet<NodeInfo> set = new();
            

            while (tmp != finish)
            {
                foreach (Node neighbor in tmp.Neighbors)
                {
                    if (!tree.ContainsKey(neighbor))
                    {
                        count += Math.Pow(1, -9);
                        NodeInfo neighborInfo = new NodeInfo((tmp.Weight + tree[tmp].Length + tmp.Danger), finish, tmp, neighbor, count, random);
                        
                        tree.Add(neighbor, neighborInfo);
                        set.Add(neighborInfo);
                    }
                    else
                    {
                        NodeInfo neighborInfo = tree[neighbor];
                        if (!neighborInfo.visited)
                        {
                            if (neighborInfo.Length > (tmp.Weight + tree[tmp].Length + tmp.Danger))
                            {
                                set.Remove(neighborInfo);
                                neighborInfo.Length = tmp.Weight + tree[tmp].Length + tmp.Danger;
                                neighborInfo.parent = tmp;
                                set.Add(neighborInfo);
                            }
                        }
                    }
                }

                if (set.Count == 0)
                {
                    Console.WriteLine("There is no path");
                    return null;
                    throw new Exception("There is no path");
                }

                NodeInfo NewtmpInfo = set.Min;
                set.Remove(NewtmpInfo);
                tmp = NewtmpInfo.self;
                NewtmpInfo.visited = true;

            }

            do
            {
                path.Add(tmp);
                tmp = tree[tmp].parent;
            }
            while (tmp != null);
            path.Reverse();
            Path fullPath = new Path(path);
            return fullPath;
        }

        public Path RandTen()
        {
            List<Node> path = new();
            Random random = new Random();
            Node temp = this.nodes[random.Next(0, this.nodes.Count - 1)];

            for (int i = 0; i < 1000; i += 1)
            {
                path.Add(temp);
                if (temp.Neighbors.Count > 1)
                {
                    do temp = path[i].Neighbors[random.Next(0, path[i].Neighbors.Count - 1)];
                    while (path[i] == temp);
                }
                else if (path[i].Neighbors.Count == 1 && path.Count > 1 && (temp = path[i].Neighbors[0]) != path[i - 1]) { }
                else
                    break;
            }
            
            return new Path(path);
        }

        public Path? FindPath(double latStart, double longStart, double latFinish, double longFinish)
        {
            Node start = NearbyNode(latStart, longStart);
            Node finish = NearbyNode(latFinish, longFinish);
            return AStar(start, finish);
        }

        public struct NewDanger
        {
            public Node point;
            public double R;
            public int dangerType;

            public NewDanger (int dangerT, double latp, double longp, double R, Graph graph)
            {
                dangerType = dangerT;
                point = graph.NearbyNode(latp, longp);
                this.R = R;
            }
        }
    }
}
