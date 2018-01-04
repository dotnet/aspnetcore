// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Blazor.UITree
{
    /// <summary>
    /// Provides methods for building a collection of <see cref="UITreeNode"/> entries.
    /// </summary>
    public class UITreeBuilder
    {
        private const int MinBufferLength = 10;
        private UITreeNode[] _entries = new UITreeNode[100];
        private int _entriesInUse = 0;
        private Stack<int> _openElementIndices = new Stack<int>();

        /// <summary>
        /// Appends a node representing an element, i.e., a container for other nodes.
        /// In order for the <see cref="UITreeBuilder"/> state to be valid, you must
        /// also call <see cref="CloseElement"/> immediately after appending the
        /// new element's child nodes.
        /// </summary>
        /// <param name="elementName">A value representing the type of the element.</param>
        public void OpenElement(string elementName)
        {
            _openElementIndices.Push(_entriesInUse);
            Append(UITreeNode.Element(elementName));
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
            => Append(UITreeNode.Text(textContent));

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
        }

        /// <summary>
        /// Returns the <see cref="UITreeNode"/> values that have been appended.
        /// The return value's <see cref="ArraySegment{T}.Offset"/> is always zero.
        /// </summary>
        /// <returns>An array segment of <see cref="UITreeNode"/> values.</returns>
        public ArraySegment<UITreeNode> GetNodes() =>
            new ArraySegment<UITreeNode>(_entries, 0, _entriesInUse);

        private void Append(UITreeNode node)
        {
            if (_entriesInUse == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            _entries[_entriesInUse++] = node;
        }
    }
}
