// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        private class DerivedIRNodeWalker : RazorIRNodeWalker
        {
            public List<RazorIRNode> Visited { get; } = new List<RazorIRNode>();

            public override void VisitDefault(RazorIRNode node)
            {
                Visited.Add(node);

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

            public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

            public override RazorIRNode Parent { get; set; }

            public override SourceSpan? Source { get; set; }

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
