// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class IRNodeWalkerTest
    {
        [Fact]
        public void IRNodeWalker_Visit_TraversesEntireGraph()
        {
            // Arrange
            var walker = new DerivedIRNodeWalker();

            var nodes = new IRNode[]
            {
                new BasicIRNode("Root"),
                    new BasicIRNode("Root->A"),
                    new BasicIRNode("Root->B"),
                        new BasicIRNode("Root->B->1"),
                        new BasicIRNode("Root->B->2"),
                    new BasicIRNode("Root->C"),
            };

            var builder = new DefaultIRBuilder();
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

        private class DerivedIRNodeWalker : IRNodeWalker
        {
            public List<IRNode> Visited { get; } = new List<IRNode>();

            public override void VisitDefault(IRNode node)
            {
                Visited.Add(node);

                base.VisitDefault(node);
            }

            public virtual void VisitBasic(BasicIRNode node)
            {
                VisitDefault(node);
            }
        }


        private class BasicIRNode : IRNode
        {
            public BasicIRNode(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override IList<IRNode> Children { get; } = new List<IRNode>();

            public override IRNode Parent { get; set; }

            public override void Accept(IRNodeVisitor visitor)
            {
                ((DerivedIRNodeWalker)visitor).VisitBasic(this);
            }

            public override TResult Accept<TResult>(IRNodeVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
