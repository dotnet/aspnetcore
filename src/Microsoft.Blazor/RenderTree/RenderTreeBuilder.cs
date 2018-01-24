// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using System;
using System.Collections.Generic;

namespace Microsoft.Blazor.RenderTree
{
    /// <summary>
    /// Provides methods for building a collection of <see cref="RenderTreeNode"/> entries.
    /// </summary>
    public class RenderTreeBuilder
    {
        private readonly Renderer _renderer;
        private readonly ArrayBuilder<RenderTreeNode> _entries = new ArrayBuilder<RenderTreeNode>(10);
        private readonly Stack<int> _openElementIndices = new Stack<int>();
        private RenderTreeNodeType? _lastNonAttributeNodeType;

        /// <summary>
        /// Constructs an instance of <see cref="RenderTreeBuilder"/>.
        /// </summary>
        /// <param name="renderer">The associated <see cref="Renderer"/>.</param>
        public RenderTreeBuilder(Renderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// Appends a node representing an element, i.e., a container for other nodes.
        /// In order for the <see cref="RenderTreeBuilder"/> state to be valid, you must
        /// also call <see cref="CloseElement"/> immediately after appending the
        /// new element's child nodes.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="elementName">A value representing the type of the element.</param>
        public void OpenElement(int sequence, string elementName)
        {
            _openElementIndices.Push(_entries.Count);
            Append(RenderTreeNode.Element(sequence, elementName));
        }

        /// <summary>
        /// Marks a previously appended element node as closed. Calls to this method
        /// must be balanced with calls to <see cref="OpenElement(string)"/>.
        /// </summary>
        public void CloseElement()
        {
            var indexOfEntryBeingClosed = _openElementIndices.Pop();
            _entries.Buffer[indexOfEntryBeingClosed].CloseElement(_entries.Count - 1);
        }

        /// <summary>
        /// Appends a node representing text content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="textContent">Content for the new text node.</param>
        public void AddText(int sequence, string textContent)
            => Append(RenderTreeNode.Text(sequence, textContent));

        /// <summary>
        /// Appends a node representing text content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="textContent">Content for the new text node.</param>
        public void AddText(int sequence, object textContent)
            => AddText(sequence, textContent?.ToString());

        /// <summary>
        /// Appends a node representing a string-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, string value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(sequence, name, value));
        }

        /// <summary>
        /// Appends a node representing an <see cref="UIEventArgs"/>-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, UIEventHandler value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(sequence, name, value));
        }

        /// <summary>
        /// Appends a node representing a string-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, object value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(sequence, name, value.ToString()));
        }

        /// <summary>
        /// Appends a node representing an attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, RenderTreeNode node)
        {
            if (node.NodeType != RenderTreeNodeType.Attribute)
            {
                throw new ArgumentException($"The {nameof(node.NodeType)} must be {RenderTreeNodeType.Attribute}.");
            }

            AssertCanAddAttribute();
            node.SetSequence(sequence);
            Append(node);
        }

        /// <summary>
        /// Appends a node representing a child component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the child component.</typeparam>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        public void OpenComponentElement<TComponent>(int sequence) where TComponent : IComponent
        {
            // Currently, child components can't have further grandchildren of their own, so it would
            // technically be possible to skip their CloseElement calls and not track them in _openElementIndices.
            // However at some point we might want to have the grandchildren nodes available at runtime
            // (rather than being parsed as attributes at compile time) so that we could have APIs for
            // components to query the complete hierarchy of transcluded nodes instead of forcing the
            // transcluded subtree to be in a particular shape such as representing key/value pairs.
            // So it's more flexible if we track open/close nodes for components explicitly.
            _openElementIndices.Push(_entries.Count);
            Append(RenderTreeNode.ChildComponent<TComponent>(sequence));
        }

        private void AssertCanAddAttribute()
        {
            if (_lastNonAttributeNodeType != RenderTreeNodeType.Element
                && _lastNonAttributeNodeType != RenderTreeNodeType.Component)
            {
                throw new InvalidOperationException($"Attributes may only be added immediately after nodes of type {RenderTreeNodeType.Element} or {RenderTreeNodeType.Component}");
            }
        }

        /// <summary>
        /// Clears the builder.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _openElementIndices.Clear();
            _lastNonAttributeNodeType = null;
        }

        /// <summary>
        /// Returns the <see cref="RenderTreeNode"/> values that have been appended.
        /// </summary>
        /// <returns>An array range of <see cref="RenderTreeNode"/> values.</returns>
        public ArrayRange<RenderTreeNode> GetNodes() =>
            _entries.ToRange();

        private void Append(in RenderTreeNode node)
        {
            _entries.Append(node);

            var nodeType = node.NodeType;
            if (nodeType != RenderTreeNodeType.Attribute)
            {
                _lastNonAttributeNodeType = node.NodeType;
            }
        }
    }
}
