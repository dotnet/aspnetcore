// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using System;

namespace Microsoft.Blazor.RenderTree
{
    // TODO: Consider coalescing properties of compatible types that don't need to be
    // used simultaneously. For example, 'ElementName' and 'AttributeName' could be replaced
    // by a single 'Name' property.

    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    public struct RenderTreeNode
    {
        /// <summary>
        /// Describes the type of this node.
        /// </summary>
        public RenderTreeNodeType NodeType { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string ElementName { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Element"/>,
        /// gets the index of the final descendant node in the tree. The value is
        /// zero if the node is of a different type, or if it has not yet been closed.
        /// </summary>
        public int ElementDescendantsEndIndex { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Text"/>,
        /// gets the content of the text node. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string TextContent { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeValue { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Attribute"/>,
        /// gets the attribute's event handle, if any. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public UIEventHandler AttributeEventHandlerValue { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Component"/>,
        /// gets the type of the child component.
        /// </summary>
        public Type ComponentType { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Component"/>,
        /// gets the child component instance identifier.
        /// </summary>
        public int ComponentId { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="RenderTreeNodeType.Component"/>,
        /// gets the child component instance. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public IComponent Component { get; private set; }

        internal static RenderTreeNode Element(string elementName) => new RenderTreeNode
        {
            NodeType = RenderTreeNodeType.Element,
            ElementName = elementName,
        };

        internal static RenderTreeNode Text(string textContent) => new RenderTreeNode
        {
            NodeType = RenderTreeNodeType.Text,
            TextContent = textContent ?? string.Empty,
        };

        internal static RenderTreeNode Attribute(string name, string value) => new RenderTreeNode
        {
            NodeType = RenderTreeNodeType.Attribute,
            AttributeName = name,
            AttributeValue = value
        };

        internal static RenderTreeNode Attribute(string name, UIEventHandler value) => new RenderTreeNode
        {
            NodeType = RenderTreeNodeType.Attribute,
            AttributeName = name,
            AttributeEventHandlerValue = value
        };

        internal static RenderTreeNode ChildComponent<T>() where T: IComponent => new RenderTreeNode
        {
            NodeType = RenderTreeNodeType.Component,
            ComponentType = typeof(T)
        };

        internal void CloseElement(int descendantsEndIndex)
        {
            ElementDescendantsEndIndex = descendantsEndIndex;
        }

        internal void SetChildComponentInstance(int componentId, IComponent component)
        {
            ComponentId = componentId;
            Component = component;
        }
    }
}
