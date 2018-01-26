// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test.Shared
{
    internal static class AssertNode
    {
        public static void Text(RenderTreeNode node, string textContent)
        {
            Assert.Equal(RenderTreeNodeType.Text, node.NodeType);
            Assert.Equal(textContent, node.TextContent);
            Assert.Equal(0, node.ElementDescendantsEndIndex);
        }

        public static void Element(RenderTreeNode node, string elementName, int descendantsEndIndex)
        {
            Assert.Equal(RenderTreeNodeType.Element, node.NodeType);
            Assert.Equal(elementName, node.ElementName);
            Assert.Equal(descendantsEndIndex, node.ElementDescendantsEndIndex);
        }

        public static void Attribute(RenderTreeNode node, string attributeName)
        {
            Assert.Equal(RenderTreeNodeType.Attribute, node.NodeType);
            Assert.Equal(attributeName, node.AttributeName);
        }

        public static void Attribute(RenderTreeNode node, string attributeName, string attributeValue)
        {
            AssertNode.Attribute(node, attributeName);
            Assert.Equal(attributeValue, node.AttributeValue);
        }

        public static void Attribute(RenderTreeNode node, string attributeName, UIEventHandler attributeEventHandlerValue)
        {
            AssertNode.Attribute(node, attributeName);
            Assert.Equal(attributeEventHandlerValue, node.AttributeValue);
        }

        public static void Component<T>(RenderTreeNode node) where T : IComponent
        {
            Assert.Equal(RenderTreeNodeType.Component, node.NodeType);
            Assert.Equal(typeof(T), node.ComponentType);
        }
    }
}
