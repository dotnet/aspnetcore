// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;
using Xunit;

namespace Microsoft.Blazor.Test
{
    public static class AssertNode
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
            Assert.Equal(attributeEventHandlerValue, node.AttributeEventHandlerValue);
        }

        public static void Component<T>(RenderTreeNode node) where T : IComponent
        {
            Assert.Equal(RenderTreeNodeType.Component, node.NodeType);

            // Currently, we instantiate child components during the tree building phase.
            // Later this will change so it happens during the tree diffing phase, so this
            // logic will need to change. It will need to verify that we're tracking the
            // information needed to instantiate the component.
            Assert.NotNull(node.Component);
            Assert.IsType<T>(node.Component);
        }
    }
}
