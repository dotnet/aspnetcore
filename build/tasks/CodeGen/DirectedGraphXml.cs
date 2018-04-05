
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;

namespace RepoTasks.CodeGen
{
    class DirectedGraphXml
    {
        private readonly XNamespace _ns = "http://schemas.microsoft.com/vs/2009/dgml";
        private readonly XDocument _doc;
        private readonly XElement _nodes;
        private readonly XElement _links;

        public DirectedGraphXml()
        {
            _doc = new XDocument(new XElement(_ns + "DirectedGraph"));
            _nodes = new XElement(_ns + "Nodes");
            _links = new XElement(_ns + "Links");
            _doc.Root.Add(_nodes);
            _doc.Root.Add(_links);
        }

        public void AddNode(string id)
        {
            _nodes.Add(new XElement(_ns + "Node", new XAttribute("Id", id), new XAttribute("Label", id)));
        }

        public void AddLink(string source, string target)
        {
            _links.Add(new XElement(_ns + "Link",
                new XAttribute("Source", source),
                new XAttribute("Target", target)));
        }

        public void Save(string path)
        {
            _doc.Save(path);
        }
    }
}
