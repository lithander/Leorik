using Leorik.Core;
using System.Text;
using System.Xml.Linq;
using static Leorik.Search.IterativeSearch;

namespace Leorik.Search
{
    public static class SearchStats
    {
        class Node
        {
            public SearchPhase Phase;
            public long Successes = 0;
            public long Failures = 0;
            public long Weight = 0;
            public List<Node> Children = new List<Node>();
        }

        static List<Node> Nodes = new List<Node>();
        static long _count = 0;

        public static void Store(SearchPhase[] stack, int ply, long delta, bool success)
        {
            _count += delta;
            Store(Nodes, stack, ply, delta, success);
        }

        private static void Store(List<Node> nodes, SearchPhase[] stack, int ply, long delta, bool success)
        {
            Node node = GetNode(nodes, stack[ply]);
            if(success)
                node.Successes += 1;
            else
                node.Failures += 1;
            node.Weight += delta;
            if (ply > 0)
                Store(node.Children, stack, ply - 1, delta, success);
        }

        private static Node GetNode(List<Node> nodes, SearchPhase searchPhase)
        {
            foreach(Node node in nodes)
            {
                if (node.Phase == searchPhase)
                    return node;
            }
            Node newNode = new Node() { Phase = searchPhase };
            nodes.Add(newNode);
            return newNode;
        }

        private static void OutputNodes(List<Node> nodes, float cutoff, int depth, Action<string> output, string key = null)
        {
            string prefix = new string(' ', depth * 2);
            nodes.Sort((a, b) => (b.Weight.CompareTo(a.Weight)));
            foreach (Node node in nodes)
            {
                string newKey = key != null ? $"{node.Phase}_{key}" : node.Phase.ToString();
                long visits = node.Successes + node.Failures;
                float pass = 100 * node.Successes / (float)(visits);
                float quant = 100 * node.Weight / (float)_count;
                if (quant > cutoff)
                {
                    output($"{prefix}{newKey}: {node.Successes} / {visits} Passed {pass:F2}% | Relevance: {quant:F2}%");
                    OutputNodes(node.Children, cutoff, depth + 1, output, newKey);
                }
            }
        }

        public static void PrintStats()
        {
            OutputNodes(Nodes, 0.1f, 0, Console.WriteLine);
        }

        public static void Dump()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"SearchStats_{timestamp}.txt";
            using var writer = new StreamWriter(filename);
            OutputNodes(Nodes, 0.1f, 0, writer.WriteLine);
        }

        public static float PredictSuccess(SearchPhase[] stack, int ply, int depth)
        {
            var nodes = Nodes;
            float pass = 0;
            int last = Math.Max(ply - depth, 0);
            for (int i = ply; i >= last; i--)
            {
                Node node = FindNode(nodes, stack[i]);
                if (node == null)
                    break;

                long visits = node.Successes + node.Failures;
                pass = 100 * node.Successes / (float)(visits);
                nodes = node.Children;
            }
            return pass;
        }

        public static void PrintStack(SearchPhase[] stack, int ply)
        {
            string line = "";
            var nodes = Nodes;
            for (int i = ply; i >= 0; i--)
            {
                Node node = FindNode(nodes, stack[i]);
                string token;
                if (node != null)
                {
                    long visits = node.Successes + node.Failures;
                    float pass = 100 * node.Successes / (float)(visits);
                    token = $"{stack[i]} {pass:F2}% #{node.Weight}";
                    nodes = node.Children;
                }
                else
                    token = stack[i].ToString();

                line = line != null ? $"{token} | {line}" : token;
            }
            Console.WriteLine(line);
        }

        private static Node FindNode(List<Node> nodes, SearchPhase searchPhase)
        {
            foreach (Node node in nodes)
            {
                if (node.Phase == searchPhase)
                    return node;
            }
            return null;
        }
    }
}
