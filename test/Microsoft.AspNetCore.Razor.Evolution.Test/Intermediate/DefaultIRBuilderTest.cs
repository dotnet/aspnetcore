// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class DefaultIRBuilderTest
    {
        [Fact]
        public void Ctor_CreatesEmptyBuilder()
        {
            // Arrange & Act
            var builder = new DefaultIRBuilder();
            var current = builder.Current;

            // Assert
            Assert.Null(current);
        }

        [Fact]
        public void Push_WhenEmpty_AddsNode()
        {
            // Arrange
            var builder = new DefaultIRBuilder();
            var node = new BasicIRNode();

            // Act
            builder.Push(node);

            // Assert
            Assert.Same(node, builder.Current);
            Assert.Null(node.Parent);
        }

        [Fact]
        public void Push_WhenNonEmpty_SetsUpParentAndChild()
        {
            // Arrange
            var builder = new DefaultIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var node = new BasicIRNode();

            // Act
            builder.Push(node);

            // Assert
            Assert.Same(node, builder.Current);
            Assert.Same(parent, node.Parent);
            Assert.Collection(parent.Children, n => Assert.Same(node, n));
        }

        [Fact]
        public void Pop_ThrowsWhenEmpty()
        {
            // Arrange
            var builder = new DefaultIRBuilder();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => builder.Pop(),
                "The 'Pop' operation is not valid when the builder is empty.");
        }

        [Fact]
        public void Pop_SingleNodeDepth_RemovesAndReturnsNode()
        {
            // Arrange
            var builder = new DefaultIRBuilder();

            var node = new BasicIRNode();
            builder.Push(node);

            // Act
            var result = builder.Pop();

            // Assert
            Assert.Same(node, result);
            Assert.Null(builder.Current);
        }

        [Fact]
        public void Pop_MultipleNodeDepth_RemovesAndReturnsNode()
        {
            // Arrange
            var builder = new DefaultIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var node = new BasicIRNode();
            builder.Push(node);

            // Act
            var result = builder.Pop();

            // Assert
            Assert.Same(node, result);
            Assert.Same(parent, builder.Current);
        }

        [Fact]
        public void Add_DoesPushAndPop()
        {
            // Arrange
            var builder = new DefaultIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var node = new BasicIRNode();

            // Act
            builder.Add(node);

            // Assert
            Assert.Same(parent, builder.Current);
            Assert.Same(parent, node.Parent);
            Assert.Collection(parent.Children, n => Assert.Same(node, n));
        }

        private class BasicIRNode : IRNode
        {
            public override IList<IRNode> Children { get; } = new List<IRNode>();

            public override IRNode Parent { get; set; }

            public override void Accept(IRNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override TResult Accept<TResult>(IRNodeVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }
        }
    }
}
