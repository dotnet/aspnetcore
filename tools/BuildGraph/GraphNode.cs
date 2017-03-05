using System.Collections.Generic;
using System.Diagnostics;

namespace BuildGraph
{
    [DebuggerDisplay("{Repository.Name}")]
    public class GraphNode
    {
        public Repository Repository { get; set; }

        public ISet<GraphNode> Incoming { get; } = new HashSet<GraphNode>();

        public ISet<GraphNode> Outgoing { get; } = new HashSet<GraphNode>();
    }
}
