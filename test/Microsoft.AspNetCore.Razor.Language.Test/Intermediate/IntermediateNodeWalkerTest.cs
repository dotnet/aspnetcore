// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class IntermediateNodeWalkerTest
    {
        [Fact]
        public void IntermediateNodeWalker_Visit_TraversesEntireGraph()
        {
            // Arrange
            var walker = new DerivedIntermediateNodeWalker();

            var nodes = new IntermediateNode[]
            {
                new BasicIntermediateNode("Root"),
                    new BasicIntermediateNode("Root->A"),
                    new BasicIntermediateNode("Root->B"),
                        new BasicIntermediateNode("Root->B->1"),
                        new BasicIntermediateNode("Root->B->2"),
                    new BasicIntermediateNode("Root->C"),
            };

            var builder = new DefaultRazorIntermediateNodeBuilder();
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
        public void IntermediateNodeWalker_Visit_SetsParentAndAncestors()
        {
            // Arrange
            var walker = new DerivedIntermediateNodeWalker();

            var nodes = new IntermediateNode[]
            {
                new BasicIntermediateNode("Root"),
                    new BasicIntermediateNode("Root->A"),
                    new BasicIntermediateNode("Root->B"),
                        new BasicIntermediateNode("Root->B->1"),
                        new BasicIntermediateNode("Root->B->2"),
                    new BasicIntermediateNode("Root->C"),
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
                Assert.Equal(ancestors[((BasicIntermediateNode)n).Name], walker.Ancestors.Cast<BasicIntermediateNode>().Select(b => b.Name));
                Assert.Equal(ancestors[((BasicIntermediateNode)n).Name].FirstOrDefault(), ((BasicIntermediateNode)walker.Parent)?.Name);
            };

            var builder = new DefaultRazorIntermediateNodeBuilder();
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

        private class DerivedIntermediateNodeWalker : IntermediateNodeWalker
        {
            public new IEnumerable<IntermediateNode> Ancestors => base.Ancestors;

            public new IntermediateNode Parent => base.Parent;
            
            public List<IntermediateNode> Visited { get; } = new List<IntermediateNode>();

            public Action<IntermediateNode> OnVisiting { get; set; }

            public override void VisitDefault(IntermediateNode node)
            {
                Visited.Add(node);

                OnVisiting?.Invoke(node);
                base.VisitDefault(node);
            }

            public virtual void VisitBasic(BasicIntermediateNode node)
            {
                VisitDefault(node);
            }
        }

        private class BasicIntermediateNode : IntermediateNode
        {
            public BasicIntermediateNode(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

            public override void Accept(IntermediateNodeVisitor visitor)
            {
                ((DerivedIntermediateNodeWalker)visitor).VisitBasic(this);
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
