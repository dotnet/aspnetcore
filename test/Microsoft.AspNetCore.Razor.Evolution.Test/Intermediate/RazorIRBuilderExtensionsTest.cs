// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class RazorIRBuilderExtensionsTest
    {
        [Fact]
        public void AddAfter_EmptyList()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();

            var node = new BasicIRNode3();

            // Act
            builder.AddAfter<BasicIRNode>(node);

            // Assert
            Assert.Collection(builder.Current.Children, n => Assert.Same(node, n));
        }

        [Fact]
        public void AddAfter_AfterMatch()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Add(new BasicIRNode());
            builder.Add(new BasicIRNode());
            builder.Add(new BasicIRNode3());

            var node = new BasicIRNode3();

            // Act
            builder.AddAfter<BasicIRNode>(node);

            // Assert
            Assert.Collection(
                builder.Current.Children,
                n => Assert.IsType<BasicIRNode>(n),
                n => Assert.IsType<BasicIRNode>(n),
                n => Assert.IsType<BasicIRNode3>(n),
                n => Assert.Same(node, n));
        }

        [Fact]
        public void AddAfter_AfterMatch_Noncontinuous()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Add(new BasicIRNode());
            builder.Add(new BasicIRNode2());
            builder.Add(new BasicIRNode());

            var node = new BasicIRNode3();

            // Act
            builder.AddAfter<BasicIRNode>(node);

            // Assert
            Assert.Collection(
                builder.Current.Children,
                n => Assert.IsType<BasicIRNode>(n),
                n => Assert.IsType<BasicIRNode2>(n),
                n => Assert.IsType<BasicIRNode>(n),
                n => Assert.Same(node, n));
        }

        private class BasicIRNode : RazorIRNode
        {
            public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

            public override RazorIRNode Parent { get; set; }

            public override SourceSpan? Source { get; set; }

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }
        }

        private class BasicIRNode2 : RazorIRNode
        {
            public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

            public override RazorIRNode Parent { get; set; }

            public override SourceSpan? Source { get; set; }

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }
        }

        private class BasicIRNode3 : RazorIRNode
        {
            public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

            public override RazorIRNode Parent { get; set; }

            public override SourceSpan? Source { get; set; }

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }
        }
    }
}
