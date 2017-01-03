// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class DefaultRazorIRBuilderTest
    {
        [Fact]
        public void Ctor_CreatesEmptyBuilder()
        {
            // Arrange & Act
            var builder = new DefaultRazorIRBuilder();
            var current = builder.Current;

            // Assert
            Assert.Null(current);
        }

        [Fact]
        public void Push_WhenEmpty_AddsNode()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();
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
            var builder = new DefaultRazorIRBuilder();

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
            var builder = new DefaultRazorIRBuilder();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => builder.Pop(),
                "The 'Pop' operation is not valid when the builder is empty.");
        }

        [Fact]
        public void Pop_SingleNodeDepth_RemovesAndReturnsNode()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

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
            var builder = new DefaultRazorIRBuilder();

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
        public void Add_AddsToChildrenAndSetsParent()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

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

        [Fact]
        public void Insert_AddsToChildrenAndSetsParent_EmptyCollection()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var node = new BasicIRNode();

            // Act
            builder.Insert(0, node);

            // Assert
            Assert.Same(parent, builder.Current);
            Assert.Same(parent, node.Parent);
            Assert.Collection(parent.Children, n => Assert.Same(node, n));
        }

        [Fact]
        public void Insert_AddsToChildrenAndSetsParent_NonEmpyCollection()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var child = new BasicIRNode();
            builder.Add(child);

            var node = new BasicIRNode();

            // Act
            builder.Insert(0, node);

            // Assert
            Assert.Same(parent, builder.Current);
            Assert.Same(parent, node.Parent);
            Assert.Collection(parent.Children, n => Assert.Same(node, n), n => Assert.Same(child, n));
        }

        [Fact]
        public void Insert_AddsToChildrenAndSetsParent_NonEmpyCollection_AtEnd()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

            var parent = new BasicIRNode();
            builder.Push(parent);

            var child = new BasicIRNode();
            builder.Add(child);

            var node = new BasicIRNode();

            // Act
            builder.Insert(1, node);

            // Assert
            Assert.Same(parent, builder.Current);
            Assert.Same(parent, node.Parent);
            Assert.Collection(parent.Children, n => Assert.Same(child, n), n => Assert.Same(node, n));
        }

        [Fact]
        public void Build_PopsMultipleLevels()
        {
            // Arrange
            var builder = new DefaultRazorIRBuilder();

            var document = new DocumentIRNode();
            builder.Push(document);

            var node = new BasicIRNode();
            builder.Push(node);

            // Act
            var result = builder.Build();

            // Assert
            Assert.Same(document, result);
            Assert.Null(builder.Current);
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
    }
}
