using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BuildGraph
{
    public class DGMLFormatter : GraphFormatter
    {
        public override void Format(IList<GraphNode> nodes, string outputPath)
        {
            var xmlns = XNamespace.Get("http://schemas.microsoft.com/vs/2009/dgml");
            var xdoc = new XDocument(
                new XElement(xmlns + "DirectedGraph",
                    new XElement(xmlns + "Nodes", GetNodes(xmlns, nodes).ToArray()),
                    new XElement(xmlns + "Links", GetLinks(xmlns, nodes).ToArray()),
                    new XElement(xmlns + "Properties", GetProperties(xmlns).ToArray())));

            using (var writer = File.OpenWrite(outputPath))
            {
                xdoc.Save(writer);
            }
        }

        private IEnumerable<XElement> GetLinks(XNamespace xmlns, IEnumerable<GraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                foreach (var outgoing in node.Outgoing)
                {
                    yield return new XElement(xmlns + "Link",
                        new XAttribute("Source", node.Repository.Name),
                        new XAttribute("Target", outgoing.Repository.Name));
                }
            }
        }

        private IEnumerable<XElement> GetNodes(XNamespace xmlns, IEnumerable<GraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return new XElement(xmlns + "Node",
                    new XAttribute("Id", node.Repository.Name),
                    new XAttribute("Label", $"{node.Repository.Name}"));
            }
        }

        private IEnumerable<XElement> GetProperties(XNamespace xmlns)
        {
            yield return new XElement(xmlns + "Property",
                new XAttribute("Id", "Label"),
                new XAttribute("Label", "Label"),
                new XAttribute("DataType", "String"));
        }
    }
}