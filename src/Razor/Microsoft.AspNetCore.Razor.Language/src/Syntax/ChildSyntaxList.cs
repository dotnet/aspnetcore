// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly struct ChildSyntaxList : IEquatable<ChildSyntaxList>, IReadOnlyList<SyntaxNode>
{
    private readonly SyntaxNode _node;
    private readonly int _count;

    internal ChildSyntaxList(SyntaxNode node)
    {
        _node = node;
        _count = CountNodes(node.Green);
    }

    /// <summary>
    /// Gets the number of children contained in the <see cref="ChildSyntaxList"/>.
    /// </summary>
    public int Count
    {
        get
        {
            return _count;
        }
    }

    internal static int CountNodes(GreenNode green)
    {
        var n = 0;

        for (int i = 0, s = green.SlotCount; i < s; i++)
        {
            var child = green.GetSlot(i);
            if (child != null)
            {
                if (!child.IsList)
                {
                    n++;
                }
                else
                {
                    n += child.SlotCount;
                }
            }
        }

        return n;
    }

    /// <summary>Gets the child at the specified index.</summary>
    /// <param name="index">The zero-based index of the child to get.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    ///   <paramref name="index"/> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="ChildSyntaxList.Count"/>. </exception>
    public SyntaxNode this[int index]
    {
        get
        {
            if (unchecked((uint)index < (uint)_count))
            {
                return ItemInternal(_node, index);
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal SyntaxNode Node
    {
        get { return _node; }
    }

    private static int Occupancy(GreenNode green)
    {
        return green.IsList ? green.SlotCount : 1;
    }

    /// <summary>
    /// internal indexer that does not verify index.
    /// Used when caller has already ensured that index is within bounds.
    /// </summary>
    internal static SyntaxNode ItemInternal(SyntaxNode node, int index)
    {
        GreenNode greenChild;
        var green = node.Green;
        var idx = index;
        var slotIndex = 0;
        var position = node.Position;

        // find a slot that contains the node or its parent list (if node is in a list)
        // we will be skipping whole slots here so we will not loop for long
        //
        // at the end of this loop we will have
        // 1) slot index - slotIdx
        // 2) if the slot is a list, node index in the list - idx
        // 3) slot position - position
        while (true)
        {
            greenChild = green.GetSlot(slotIndex);
            if (greenChild != null)
            {
                var currentOccupancy = Occupancy(greenChild);
                if (idx < currentOccupancy)
                {
                    break;
                }

                idx -= currentOccupancy;
                position += greenChild.FullWidth;
            }

            slotIndex++;
        }

        // get node that represents this slot
        var red = node.GetNodeSlot(slotIndex);
        if (!greenChild.IsList)
        {
            // this is a single node
            // if it is a node, we are done
            if (red != null)
            {
                return red;
            }
        }
        else if (red != null)
        {
            // it is a red list of nodes (separated or not), most common case
            var redChild = red.GetNodeSlot(idx);
            if (redChild != null)
            {
                // this is our node
                return redChild;
            }
        }

        return node;
    }

    /// <summary>
    /// Locate the node that is a child of the given <see cref="SyntaxNode"/> and contains the given position.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to search.</param>
    /// <param name="targetPosition">The position.</param>
    /// <returns>The node that spans the given position.</returns>
    /// <remarks>
    /// Assumes that <paramref name="targetPosition"/> is within the span of <paramref name="node"/>.
    /// </remarks>
    internal static SyntaxNode ChildThatContainsPosition(SyntaxNode node, int targetPosition)
    {
        // The targetPosition must already be within this node
        Debug.Assert(node.FullSpan.Contains(targetPosition));

        var green = node.Green;
        var position = node.Position;
        var index = 0;

        Debug.Assert(!green.IsList);

        // Find the green node that spans the target position.
        // We will be skipping whole slots here so we will not loop for long
        int slot;
        for (slot = 0; ; slot++)
        {
            var greenChild = green.GetSlot(slot);
            if (greenChild != null)
            {
                var endPosition = position + greenChild.FullWidth;
                if (targetPosition < endPosition)
                {
                    // Descend into the child element
                    green = greenChild;
                    break;
                }

                position = endPosition;
                index += Occupancy(greenChild);
            }
        }

        // Realize the red node (if any)
        var red = node.GetNodeSlot(slot);
        if (!green.IsList)
        {
            // This is a single node.
            // If it is a node, we are done.
            if (red != null)
            {
                return red;
            }
        }
        else
        {
            slot = green.FindSlotIndexContainingOffset(targetPosition - position);

            // Realize the red node (if any)
            if (red != null)
            {
                // It is a red list of nodes
                red = red.GetNodeSlot(slot);
                if (red != null)
                {
                    return red;
                }
            }

            // Since we can't have "lists of lists", the Occupancy calculation for
            // child elements in a list is simple.
            index += slot;
        }

        return node;
    }

    /// <summary>
    /// internal indexer that does not verify index.
    /// Used when caller has already ensured that index is within bounds.
    /// </summary>
    internal static SyntaxNode ItemInternalAsNode(SyntaxNode node, int index)
    {
        GreenNode greenChild;
        var green = node.Green;
        var idx = index;
        var slotIndex = 0;

        // find a slot that contains the node or its parent list (if node is in a list)
        // we will be skipping whole slots here so we will not loop for long
        //
        // at the end of this loop we will have
        // 1) slot index - slotIdx
        // 2) if the slot is a list, node index in the list - idx
        while (true)
        {
            greenChild = green.GetSlot(slotIndex);
            if (greenChild != null)
            {
                var currentOccupancy = Occupancy(greenChild);
                if (idx < currentOccupancy)
                {
                    break;
                }

                idx -= currentOccupancy;
            }

            slotIndex++;
        }

        // get node that represents this slot
        var red = node.GetNodeSlot(slotIndex);
        if (greenChild.IsList && red != null)
        {
            // it is a red list of nodes, most common case
            return red.GetNodeSlot(idx);
        }

        // this is a single node
        return red;
    }

    // for debugging
    private SyntaxNode[] Nodes
    {
        get
        {
            return this.ToArray();
        }
    }

    public bool Any()
    {
        return _count != 0;
    }

    /// <summary>
    /// Returns the first child in the list.
    /// </summary>
    /// <returns>The first child in the list.</returns>
    /// <exception cref="System.InvalidOperationException">The list is empty.</exception>
    public SyntaxNode First()
    {
        if (Any())
        {
            return this[0];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Returns the last child in the list.
    /// </summary>
    /// <returns>The last child in the list.</returns>
    /// <exception cref="System.InvalidOperationException">The list is empty.</exception>
    public SyntaxNode Last()
    {
        if (Any())
        {
            return this[_count - 1];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Returns a list which contains all children of <see cref="ChildSyntaxList"/> in reversed order.
    /// </summary>
    /// <returns><see cref="Reversed"/> which contains all children of <see cref="ChildSyntaxList"/> in reversed order</returns>
    public Reversed Reverse()
    {
        return new Reversed(_node, _count);
    }

    /// <summary>Returns an enumerator that iterates through the <see cref="ChildSyntaxList"/>.</summary>
    /// <returns>A <see cref="Enumerator"/> for the <see cref="ChildSyntaxList"/>.</returns>
    public Enumerator GetEnumerator()
    {
        if (_node == null)
        {
            return default;
        }

        return new Enumerator(_node, _count);
    }

    IEnumerator<SyntaxNode> IEnumerable<SyntaxNode>.GetEnumerator()
    {
        if (_node == null)
        {
            return EmptyEnumerator<SyntaxNode>.Instance;
        }

        return new EnumeratorImpl(_node, _count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (_node == null)
        {
            return EmptyEnumerator<SyntaxNode>.Instance;
        }

        return new EnumeratorImpl(_node, _count);
    }

    /// <summary>Determines whether the specified object is equal to the current instance.</summary>
    /// <returns>true if the specified object is a <see cref="ChildSyntaxList" /> structure and is equal to the current instance; otherwise, false.</returns>
    /// <param name="obj">The object to be compared with the current instance.</param>
    public override bool Equals(object obj)
    {
        return obj is ChildSyntaxList && Equals((ChildSyntaxList)obj);
    }

    /// <summary>Determines whether the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance.</summary>
    /// <returns>true if the specified <see cref="ChildSyntaxList" /> structure is equal to the current instance; otherwise, false.</returns>
    /// <param name="other">The <see cref="ChildSyntaxList" /> structure to be compared with the current instance.</param>
    public bool Equals(ChildSyntaxList other)
    {
        return _node == other._node;
    }

    /// <summary>Returns the hash code for the current instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return _node?.GetHashCode() ?? 0;
    }

    /// <summary>Indicates whether two <see cref="ChildSyntaxList" /> structures are equal.</summary>
    /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
    /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the equality operator.</param>
    /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the equality operator.</param>
    public static bool operator ==(ChildSyntaxList list1, ChildSyntaxList list2)
    {
        return list1.Equals(list2);
    }

    /// <summary>Indicates whether two <see cref="ChildSyntaxList" /> structures are unequal.</summary>
    /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
    /// <param name="list1">The <see cref="ChildSyntaxList" /> structure on the left side of the inequality operator.</param>
    /// <param name="list2">The <see cref="ChildSyntaxList" /> structure on the right side of the inequality operator.</param>
    public static bool operator !=(ChildSyntaxList list1, ChildSyntaxList list2)
    {
        return !list1.Equals(list2);
    }

    /// <summary>Enumerates the elements of a <see cref="ChildSyntaxList" />.</summary>
    public struct Enumerator
    {
        private SyntaxNode _node;
        private int _count;
        private int _childIndex;

        internal Enumerator(SyntaxNode node, int count)
        {
            _node = node;
            _count = count;
            _childIndex = -1;
        }

        // PERF: Initialize an Enumerator directly from a SyntaxNode without going
        // via ChildNodes. This saves constructing an intermediate ChildSyntaxList
        internal void InitializeFrom(SyntaxNode node)
        {
            _node = node;
            _count = CountNodes(node.Green);
            _childIndex = -1;
        }

        /// <summary>Advances the enumerator to the next element of the <see cref="ChildSyntaxList" />.</summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            var newIndex = _childIndex + 1;
            if (newIndex < _count)
            {
                _childIndex = newIndex;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        /// <returns>The element in the <see cref="ChildSyntaxList" /> at the current position of the enumerator.</returns>
        public SyntaxNode Current
        {
            get
            {
                return ItemInternal(_node, _childIndex);
            }
        }

        /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
        public void Reset()
        {
            _childIndex = -1;
        }

        internal bool TryMoveNextAndGetCurrent(out SyntaxNode current)
        {
            if (!MoveNext())
            {
                current = default;
                return false;
            }

            current = ItemInternal(_node, _childIndex);
            return true;
        }

        internal SyntaxNode TryMoveNextAndGetCurrentAsNode()
        {
            while (MoveNext())
            {
                var nodeValue = ItemInternalAsNode(_node, _childIndex);
                if (nodeValue != null)
                {
                    return nodeValue;
                }
            }

            return null;
        }
    }

    private class EnumeratorImpl : IEnumerator<SyntaxNode>
    {
        private Enumerator _enumerator;

        internal EnumeratorImpl(SyntaxNode node, int count)
        {
            _enumerator = new Enumerator(node, count);
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        ///   </returns>
        public SyntaxNode Current
        {
            get { return _enumerator.Current; }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        ///   </returns>
        object IEnumerator.Current
        {
            get { return _enumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        { }
    }

    public readonly partial struct Reversed : IEnumerable<SyntaxNode>, IEquatable<Reversed>
    {
        private readonly SyntaxNode _node;
        private readonly int _count;

        internal Reversed(SyntaxNode node, int count)
        {
            _node = node;
            _count = count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_node, _count);
        }

        IEnumerator<SyntaxNode> IEnumerable<SyntaxNode>.GetEnumerator()
        {
            if (_node == null)
            {
                return EmptyEnumerator<SyntaxNode>.Instance;
            }

            return new EnumeratorImpl(_node, _count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_node == null)
            {
                return EmptyEnumerator<SyntaxNode>.Instance;
            }

            return new EnumeratorImpl(_node, _count);
        }

        public override int GetHashCode()
        {
            if (_node == null)
            {
                return 0;
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(_node.GetHashCode());
            hash.Add(_count);
            return hash.CombinedHash;
        }

        public override bool Equals(object obj)
        {
            return (obj is Reversed) && Equals((Reversed)obj);
        }

        public bool Equals(Reversed other)
        {
            return _node == other._node
                && _count == other._count;
        }

        public struct Enumerator
        {
            private readonly SyntaxNode _node;
            private readonly int _count;
            private int _childIndex;

            internal Enumerator(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
                _childIndex = count;
            }

            public bool MoveNext()
            {
                return --_childIndex >= 0;
            }

            public SyntaxNode Current
            {
                get
                {
                    return ItemInternal(_node, _childIndex);
                }
            }

            public void Reset()
            {
                _childIndex = _count;
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxNode>
        {
            private Enumerator _enumerator;

            internal EnumeratorImpl(SyntaxNode node, int count)
            {
                _enumerator = new Enumerator(node, count);
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            ///   </returns>
            public SyntaxNode Current
            {
                get { return _enumerator.Current; }
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            ///   </returns>
            object IEnumerator.Current
            {
                get { return _enumerator.Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
            {
                _enumerator.Reset();
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            { }
        }
    }

    internal class EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly IEnumerator<T> Instance = new EmptyEnumerator<T>();

        protected EmptyEnumerator()
        {
        }

        public T Current => throw new InvalidOperationException();

        object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
