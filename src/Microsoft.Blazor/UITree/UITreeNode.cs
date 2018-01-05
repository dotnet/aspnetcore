// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.UITree
{
    // TODO: Consider coalescing properties of compatible types that don't need to be
    // used simultaneously. For example, 'ElementName' and 'AttributeName' could be replaced
    // by a single 'Name' property.

    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    public struct UITreeNode
    {
        /// <summary>
        /// Describes the type of this node.
        /// </summary>
        public UITreeNodeType NodeType { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="UITreeNodeType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string ElementName { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="UITreeNodeType.Element"/>,
        /// gets the index of the final descendant node in the tree. The value is
        /// zero if the node is of a different type, or if it has not yet been closed.
        /// </summary>
        public int ElementDescendantsEndIndex { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="UITreeNodeType.Text"/>,
        /// gets the content of the text node. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string TextContent { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="UITreeNodeType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// If the <see cref="NodeType"/> property equals <see cref="UITreeNodeType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeValue { get; private set; }

        internal static UITreeNode Element(string elementName) => new UITreeNode
        {
            NodeType = UITreeNodeType.Element,
            ElementName = elementName,
        };

        internal static UITreeNode Text(string textContent) => new UITreeNode
        {
            NodeType = UITreeNodeType.Text,
            TextContent = textContent,
        };

        internal static UITreeNode Attribute(string name, string value) => new UITreeNode
        {
            NodeType = UITreeNodeType.Attribute,
            AttributeName = name,
            AttributeValue = value
        };

        internal void CloseElement(int descendantsEndIndex)
        {
            ElementDescendantsEndIndex = descendantsEndIndex;
        }
    }
}
