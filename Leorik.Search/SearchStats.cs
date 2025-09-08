using Leorik.Core;
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

        public static void PrintStats()
        {
            PrintNodes(Nodes, 0.1f, 0);
        }

        private static void PrintNodes(List<Node> nodes, float cutoff, int depth, string key = null)
        {
            string prefix = new string(' ', depth * 2);
            nodes.Sort((a, b) => (b.Weight.CompareTo(a.Weight)));
            foreach(Node node in nodes)
            {
                string newKey = key != null ? $"{node.Phase}_{key}" : node.Phase.ToString();
                long visits = node.Successes + node.Failures;
                float pass = 100 * node.Successes / (float)(visits);
                float quant = 100 * node.Weight / (float)_count;
                if(quant > cutoff)
                {
                    Console.WriteLine($"{prefix}{newKey}: {node.Successes} / {visits} Passed {pass:F2}% | Relevance: {quant:F2}%");
                    PrintNodes(node.Children, cutoff, depth + 1, newKey);
                }
            }
        }
    }
}
