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
        private const int MinBufferLength = 10;
        private readonly Renderer _renderer;
        private RenderTreeNode[] _entries = new RenderTreeNode[100];
        private int _entriesInUse = 0;
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
        /// <param name="elementName">A value representing the type of the element.</param>
        public void OpenElement(string elementName)
        {
            _openElementIndices.Push(_entriesInUse);
            Append(RenderTreeNode.Element(elementName));
        }

        /// <summary>
        /// Marks a previously appended element node as closed. Calls to this method
        /// must be balanced with calls to <see cref="OpenElement(string)"/>.
        /// </summary>
        public void CloseElement()
        {
            var indexOfEntryBeingClosed = _openElementIndices.Pop();
            _entries[indexOfEntryBeingClosed].CloseElement(_entriesInUse - 1);
        }

        /// <summary>
        /// Appends a node representing text content.
        /// </summary>
        /// <param name="textContent">Content for the new text node.</param>
        public void AddText(string textContent)
            => Append(RenderTreeNode.Text(textContent));

        /// <summary>
        /// Appends a node representing text content.
        /// </summary>
        /// <param name="textContent">Content for the new text node.</param>
        public void AddText(object textContent)
            => AddText(textContent?.ToString());

        /// <summary>
        /// Appends a node representing a string-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(string name, string value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(name, value));
        }

        /// <summary>
        /// Appends a node representing an <see cref="UIEventArgs"/>-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(string name, UIEventHandler value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(name, value));
        }

        /// <summary>
        /// Appends a node representing a string-valued attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(string name, object value)
        {
            AssertCanAddAttribute();
            Append(RenderTreeNode.Attribute(name, value.ToString()));
        }

        /// <summary>
        /// Appends a node representing an attribute.
        /// The attribute is associated with the most recently added element.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(RenderTreeNode node)
        {
            if (node.NodeType != RenderTreeNodeType.Attribute)
            {
                throw new ArgumentException($"The {nameof(node.NodeType)} must be {RenderTreeNodeType.Attribute}.");
            }

            AssertCanAddAttribute();
            Append(node);
        }

        /// <summary>
        /// Appends a node representing a child component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the child component.</typeparam>
        public void AddComponent<TComponent>() where TComponent: IComponent
        {
            // Later, instead of instantiating the child component here, we'll instead
            // store a descriptor of the component (type, parameters) on the attributes
            // of the appended nodes. Then after the tree is diffed against the
            // previous tree, we'll either instantiate a new component or reuse the
            // existing instance (and notify it about changes to parameters).
            var instance = Activator.CreateInstance<TComponent>();
            var instanceId = _renderer.AssignComponentId(instance);
            Append(RenderTreeNode.ChildComponent(instanceId, instance));
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
            // If the previous usage of the buffer showed that we have allocated
            // much more space than needed, free up the excess memory
            var shrinkToLength = Math.Max(MinBufferLength, _entries.Length / 2);
            if (_entriesInUse < shrinkToLength)
            {
                Array.Resize(ref _entries, shrinkToLength);
            }

            _entriesInUse = 0;
            _openElementIndices.Clear();
            _lastNonAttributeNodeType = null;
        }

        /// <summary>
        /// Returns the <see cref="RenderTreeNode"/> values that have been appended.
        /// The return value's <see cref="ArraySegment{T}.Offset"/> is always zero.
        /// </summary>
        /// <returns>An array segment of <see cref="RenderTreeNode"/> values.</returns>
        public ArraySegment<RenderTreeNode> GetNodes() =>
            new ArraySegment<RenderTreeNode>(_entries, 0, _entriesInUse);

        private void Append(RenderTreeNode node)
        {
            if (_entriesInUse == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            _entries[_entriesInUse++] = node;

            var nodeType = node.NodeType;
            if (nodeType != RenderTreeNodeType.Attribute)
            {
                _lastNonAttributeNodeType = node.NodeType;
            }
        }
    }
}
