// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal abstract class SyntaxList : GreenNode
{
    internal SyntaxList()
        : base(SyntaxKind.List)
    {
    }

    internal SyntaxList(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
        : base(SyntaxKind.List, diagnostics, annotations)
    {
    }

    internal override bool IsList => true;

    internal static GreenNode List(GreenNode child)
    {
        return child;
    }

    internal static WithTwoChildren List(GreenNode child0, GreenNode child1)
    {
        Debug.Assert(child0 != null);
        Debug.Assert(child1 != null);

        var result = new WithTwoChildren(child0, child1);
        return result;
    }

    internal static WithThreeChildren List(GreenNode child0, GreenNode child1, GreenNode child2)
    {
        Debug.Assert(child0 != null);
        Debug.Assert(child1 != null);
        Debug.Assert(child2 != null);

        var result = new WithThreeChildren(child0, child1, child2);
        return result;
    }

    internal static GreenNode List(GreenNode[] nodes)
    {
        return List(nodes, nodes.Length);
    }

    internal static GreenNode List(GreenNode[] nodes, int count)
    {
        var array = new ArrayElement<GreenNode>[count];
        for (int i = 0; i < count; i++)
        {
            Debug.Assert(nodes[i] != null);
            array[i].Value = nodes[i];
        }

        return List(array);
    }

    internal static SyntaxList List(ArrayElement<GreenNode>[] children)
    {
        // "WithLotsOfChildren" list will allocate a separate array to hold
        // precomputed node offsets. It may not be worth it for smallish lists.
        if (children.Length < 10)
        {
            return new WithManyChildren(children);
        }
        else
        {
            return new WithLotsOfChildren(children);
        }
    }

    internal abstract void CopyTo(ArrayElement<GreenNode>[] array, int offset);

    internal static GreenNode Concat(GreenNode left, GreenNode right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        var leftList = left as SyntaxList;
        var rightList = right as SyntaxList;
        if (leftList != null)
        {
            if (rightList != null)
            {
                var tmp = new ArrayElement<GreenNode>[left.SlotCount + right.SlotCount];
                leftList.CopyTo(tmp, 0);
                rightList.CopyTo(tmp, left.SlotCount);
                return List(tmp);
            }
            else
            {
                var tmp = new ArrayElement<GreenNode>[left.SlotCount + 1];
                leftList.CopyTo(tmp, 0);
                tmp[left.SlotCount].Value = right;
                return List(tmp);
            }
        }
        else if (rightList != null)
        {
            var tmp = new ArrayElement<GreenNode>[rightList.SlotCount + 1];
            tmp[0].Value = left;
            rightList.CopyTo(tmp, 1);
            return List(tmp);
        }
        else
        {
            return List(left, right);
        }
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.Visit(this);
    }

    internal class WithTwoChildren : SyntaxList
    {
        private readonly GreenNode _child0;
        private readonly GreenNode _child1;

        internal WithTwoChildren(GreenNode child0, GreenNode child1)
        {
            SlotCount = 2;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
        }

        internal WithTwoChildren(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations, GreenNode child0, GreenNode child1)
        {
            SlotCount = 2;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
        }

        internal override GreenNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return _child0;
                case 1:
                    return _child1;
                default:
                    return null;
            }
        }

        internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
        {
            array[offset].Value = _child0;
            array[offset + 1].Value = _child1;
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.SyntaxList.WithTwoChildren(this, parent, position);
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] errors)
        {
            return new WithTwoChildren(errors, this.GetAnnotations(), _child0, _child1);
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new WithTwoChildren(GetDiagnostics(), annotations, _child0, _child1);
        }
    }

    internal class WithThreeChildren : SyntaxList
    {
        private readonly GreenNode _child0;
        private readonly GreenNode _child1;
        private readonly GreenNode _child2;

        internal WithThreeChildren(GreenNode child0, GreenNode child1, GreenNode child2)
        {
            SlotCount = 3;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
            AdjustFlagsAndWidth(child2);
            _child2 = child2;
        }

        internal WithThreeChildren(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations, GreenNode child0, GreenNode child1, GreenNode child2)
            : base(diagnostics, annotations)
        {
            SlotCount = 3;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
            AdjustFlagsAndWidth(child2);
            _child2 = child2;
        }

        internal override GreenNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return _child0;
                case 1:
                    return _child1;
                case 2:
                    return _child2;
                default:
                    return null;
            }
        }

        internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
        {
            array[offset].Value = _child0;
            array[offset + 1].Value = _child1;
            array[offset + 2].Value = _child2;
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.SyntaxList.WithThreeChildren(this, parent, position);
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] errors)
        {
            return new WithThreeChildren(errors, GetAnnotations(), _child0, _child1, _child2);
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new WithThreeChildren(GetDiagnostics(), annotations, _child0, _child1, _child2);
        }
    }

    internal abstract class WithManyChildrenBase : SyntaxList
    {
        internal readonly ArrayElement<GreenNode>[] children;

        internal WithManyChildrenBase(ArrayElement<GreenNode>[] children)
        {
            this.children = children;
            this.InitializeChildren();
        }

        internal WithManyChildrenBase(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<GreenNode>[] children)
            : base(diagnostics, annotations)
        {
            this.children = children;
            this.InitializeChildren();
        }

        private void InitializeChildren()
        {
            var n = children.Length;
            if (n < byte.MaxValue)
            {
                SlotCount = (byte)n;
            }
            else
            {
                SlotCount = byte.MaxValue;
            }

            for (var i = 0; i < children.Length; i++)
            {
                AdjustFlagsAndWidth(children[i]);
            }
        }

        protected override int GetSlotCount()
        {
            return children.Length;
        }

        internal override GreenNode GetSlot(int index)
        {
            return children[index];
        }

        internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
        {
            Array.Copy(children, 0, array, offset, children.Length);
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.SyntaxList.WithManyChildren(this, parent, position);
        }
    }

    internal sealed class WithManyChildren : WithManyChildrenBase
    {
        internal WithManyChildren(ArrayElement<GreenNode>[] children)
            : base(children)
        {
        }

        internal WithManyChildren(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<GreenNode>[] children)
            : base(diagnostics, annotations, children)
        {
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] errors)
        {
            return new WithManyChildren(errors, GetAnnotations(), children);
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new WithManyChildren(GetDiagnostics(), annotations, children);
        }
    }

    internal sealed class WithLotsOfChildren : WithManyChildrenBase
    {
        private readonly int[] _childOffsets;

        internal WithLotsOfChildren(ArrayElement<GreenNode>[] children)
            : base(children)
        {
            _childOffsets = CalculateOffsets(children);
        }

        internal WithLotsOfChildren(RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<GreenNode>[] children, int[] childOffsets)
            : base(diagnostics, annotations, children)
        {
            _childOffsets = childOffsets;
        }

        public override int GetSlotOffset(int index)
        {
            return _childOffsets[index];
        }

        /// <summary>
        /// Find the slot that contains the given offset.
        /// </summary>
        /// <param name="offset">The target offset. Must be between 0 and <see cref="GreenNode.FullWidth"/>.</param>
        /// <returns>The slot index of the slot containing the given offset.</returns>
        /// <remarks>
        /// This implementation uses a binary search to find the first slot that contains
        /// the given offset.
        /// </remarks>
        public override int FindSlotIndexContainingOffset(int offset)
        {
            Debug.Assert(offset >= 0 && offset < FullWidth);
            return BinarySearchUpperBound(_childOffsets, offset) - 1;
        }

        private static int[] CalculateOffsets(ArrayElement<GreenNode>[] children)
        {
            var n = children.Length;
            var childOffsets = new int[n];
            var offset = 0;
            for (var i = 0; i < n; i++)
            {
                childOffsets[i] = offset;
                offset += children[i].Value.FullWidth;
            }
            return childOffsets;
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] errors)
        {
            return new WithLotsOfChildren(errors, this.GetAnnotations(), children, _childOffsets);
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new WithLotsOfChildren(GetDiagnostics(), annotations, children, _childOffsets);
        }

        /// <summary>
        /// Search a sorted integer array for the target value in O(log N) time.
        /// </summary>
        /// <param name="array">The array of integers which must be sorted in ascending order.</param>
        /// <param name="value">The target value.</param>
        /// <returns>An index in the array pointing to the position where <paramref name="value"/> should be
        /// inserted in order to maintain the sorted order. All values to the right of this position will be
        /// strictly greater than <paramref name="value"/>. Note that this may return a position off the end
        /// of the array if all elements are less than or equal to <paramref name="value"/>.</returns>
        private static int BinarySearchUpperBound(int[] array, int value)
        {
            var low = 0;
            var high = array.Length - 1;

            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                if (array[middle] > value)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }
    }
}
