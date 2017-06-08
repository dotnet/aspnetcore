// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class RazorIRNodeWalkerTest
    {
        [Fact]
        public void IRNodeWalker_Visit_TraversesEntireGraph()
        {
            // Arrange
            var walker = new DerivedIRNodeWalker();

            var nodes = new RazorIRNode[]
            {
                new BasicIRNode("Root"),
                    new BasicIRNode("Root->A"),
                    new BasicIRNode("Root->B"),
                        new BasicIRNode("Root->B->1"),
                        new BasicIRNode("Root->B->2"),
                    new BasicIRNode("Root->C"),
            };

            var builder = new DefaultRazorIRBuilder();
            builder.Push(nodes[0]);
            builder.Add(nodes[1]);
            builder.Push(nodes[2]);
            builder.Add(nodes[3]);
            builder.Add(nodes[4]);
            builder.Pop();
            builder.Add(nodes[5]);

            var root = builder.Pop();

            // Act
            walker.Visit(root);

            // Assert
            Assert.Equal(nodes, walker.Visited.ToArray());
        }

        [Fact]
        public void IRNodeWalker_Visit_SetsParentAndAncestors()
        {
            // Arrange
            var walker = new DerivedIRNodeWalker();

            var nodes = new RazorIRNode[]
            {
                new BasicIRNode("Root"),
                    new BasicIRNode("Root->A"),
                    new BasicIRNode("Root->B"),
                        new BasicIRNode("Root->B->1"),
                        new BasicIRNode("Root->B->2"),
                    new BasicIRNode("Root->C"),
            };

            var ancestors = new Dictionary<string, string[]>()
            {
                { "Root", new string[]{ } },
                { "Root->A", new string[] { "Root" } },
                { "Root->B", new string[] { "Root" } },
                { "Root->B->1", new string[] { "Root->B", "Root" } },
                { "Root->B->2", new string[] { "Root->B", "Root" } },
                { "Root->C", new string[] { "Root" } },
            };

            walker.OnVisiting = (n) =>
            {
                Assert.Equal(ancestors[((BasicIRNode)n).Name], walker.Ancestors.Cast<BasicIRNode>().Select(b => b.Name));
                Assert.Equal(ancestors[((BasicIRNode)n).Name].FirstOrDefault(), ((BasicIRNode)walker.Parent)?.Name);
            };

            var builder = new DefaultRazorIRBuilder();
            builder.Push(nodes[0]);
            builder.Add(nodes[1]);
            builder.Push(nodes[2]);
            builder.Add(nodes[3]);
            builder.Add(nodes[4]);
            builder.Pop();
            builder.Add(nodes[5]);

            var root = builder.Pop();

            // Act & Assert
            walker.Visit(root);
        }

        private class DerivedIRNodeWalker : RazorIRNodeWalker
        {
            public new IEnumerable<RazorIRNode> Ancestors => base.Ancestors;

            public new RazorIRNode Parent => base.Parent;
            
            public List<RazorIRNode> Visited { get; } = new List<RazorIRNode>();

            public Action<RazorIRNode> OnVisiting { get; set; }

            public override void VisitDefault(RazorIRNode node)
            {
                Visited.Add(node);

                OnVisiting?.Invoke(node);
                base.VisitDefault(node);
            }

            public virtual void VisitBasic(BasicIRNode node)
            {
                VisitDefault(node);
            }
        }

        private class BasicIRNode : RazorIRNode
        {
            public BasicIRNode(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override ItemCollection Annotations { get; } = new DefaultItemCollection();

            public override RazorDiagnosticCollection Diagnostics { get; } = new DefaultDiagnosticCollection();
            
            public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

            public override SourceSpan? Source { get; set; }

            public override bool HasDiagnostics => Diagnostics.Count > 0;

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                ((DerivedIRNodeWalker)visitor).VisitBasic(this);
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
