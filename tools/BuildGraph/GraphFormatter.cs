using System.Collections.Generic;

namespace BuildGraph
{
    public abstract class GraphFormatter
    {
        public abstract void Format(IList<GraphNode> nodes, string outputPath);
    }
}