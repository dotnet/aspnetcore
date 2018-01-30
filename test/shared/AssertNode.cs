// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test.Shared
{
    internal static class AssertNode
    {
        public static void Sequence(RenderTreeNode node, int? sequence = null)
        {
            if (sequence.HasValue)
            {
                Assert.Equal(sequence.Value, node.Sequence);
            }
        }

        public static void Text(RenderTreeNode node, string textContent, int? sequence = null)
        {
            Assert.Equal(RenderTreeNodeType.Text, node.NodeType);
            Assert.Equal(textContent, node.TextContent);
            Assert.Equal(0, node.ElementDescendantsEndIndex);
            AssertNode.Sequence(node, sequence);
        }

        public static void Element(RenderTreeNode node, string elementName, int descendantsEndIndex, int? sequence = null)
        {
            Assert.Equal(RenderTreeNodeType.Element, node.NodeType);
            Assert.Equal(elementName, node.ElementName);
            Assert.Equal(descendantsEndIndex, node.ElementDescendantsEndIndex);
            AssertNode.Sequence(node, sequence);
        }

        public static void Attribute(RenderTreeNode node, string attributeName, int? sequence = null)
        {
            Assert.Equal(RenderTreeNodeType.Attribute, node.NodeType);
            Assert.Equal(attributeName, node.AttributeName);
            AssertNode.Sequence(node, sequence);
        }

        public static void Attribute(RenderTreeNode node, string attributeName, string attributeValue, int? sequence = null)
        {
            AssertNode.Attribute(node, attributeName, sequence);
            Assert.Equal(attributeValue, node.AttributeValue);
        }

        public static void Attribute(RenderTreeNode node, string attributeName, UIEventHandler attributeEventHandlerValue, int? sequence = null)
        {
            AssertNode.Attribute(node, attributeName, sequence);
            Assert.Equal(attributeEventHandlerValue, node.AttributeValue);
        }

        public static void Component<T>(RenderTreeNode node, int? sequence = null) where T : IComponent
        {
            Assert.Equal(RenderTreeNodeType.Component, node.NodeType);
            Assert.Equal(typeof(T), node.ComponentType);
            AssertNode.Sequence(node, sequence);
        }

        public static void Whitespace(RenderTreeNode node, int? sequence = null)
        {
            Assert.Equal(RenderTreeNodeType.Text, node.NodeType);
            AssertNode.Sequence(node, sequence);
            Assert.True(string.IsNullOrWhiteSpace(node.TextContent));
        }
    }
}
