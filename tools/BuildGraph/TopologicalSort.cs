using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGraph
{
    public class TopologicalSort : IComparer<GraphNode>
    {
        public static readonly TopologicalSort Instance = new TopologicalSort();

        public int Compare(GraphNode x, GraphNode y)
        {
            var xScore = GetOrder(x);
            var yScore = GetOrder(y);
            return xScore.CompareTo(yScore);
        }

        public static int GetOrder(GraphNode node)
        {
            var visited = new List<GraphNode>();
            return GetOrder(node, visited);
        }

        private static int GetOrder(GraphNode node, List<GraphNode> visited)
        {
            if (visited.Contains(node))
            {
                var cycle = string.Join(" -> ", visited.Select(v => v.Repository.Name));
                throw new Exception($"Cycle detected in the build graph: {cycle} -> {node.Repository.Name}.");
            }

            var score = 0;
            visited.Add(node);
            foreach (var dependentNode in node.Incoming)
            {
                score = Math.Max(score, GetOrder(dependentNode, visited));
            }
            visited.RemoveAt(visited.Count - 1);

            return score + 1;
        }
    }
}
