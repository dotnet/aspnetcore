using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BuildGraph
{
    public class MSBuildGraphFormatter : GraphFormatter
    {
        public override void Format(IList<GraphNode> nodes, string outputPath)
        {
            var sortedNodes = nodes.Select(node => new { Repository = node.Repository, Order = TopologicalSort.GetOrder(node) })
                .OrderBy(item => item.Order);
            var projectElement = new XElement("Project",
                new XElement("ItemGroup",
                    sortedNodes.Select(item => new XElement("RepositoryToBuildInOrder",
                        new XAttribute("Include", item.Repository.Name),
                        new XAttribute("Order", item.Order)))));

            File.WriteAllText(outputPath, projectElement.ToString());
        }
    }
}