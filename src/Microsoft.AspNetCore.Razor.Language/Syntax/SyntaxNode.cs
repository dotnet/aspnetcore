// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract partial class SyntaxNode
    {
        public SyntaxNode(GreenNode green, SyntaxNode parent, int position)
        {
            Green = green;
            Parent = parent;
            Position = position;
        }

        internal GreenNode Green { get; }

        public SyntaxNode Parent { get; }

        public int Position { get; }

        public int EndPosition => Position + FullWidth;

        public SyntaxKind Kind => Green.Kind;

        public int Width => Green.Width;

        public int FullWidth => Green.FullWidth;

        public int SpanStart => Position + Green.GetLeadingTriviaWidth();

        public TextSpan FullSpan => new TextSpan(Position, Green.FullWidth);

        public TextSpan Span
        {
            get
            {
                // Start with the full span.
                var start = Position;
                var width = Green.FullWidth;

                // adjust for preceding trivia (avoid calling this twice, do not call Green.Width)
                var precedingWidth = Green.GetLeadingTriviaWidth();
                start += precedingWidth;
                width -= precedingWidth;

                // adjust for following trivia width
                width -= Green.GetTrailingTriviaWidth();

                Debug.Assert(width >= 0);
                return new TextSpan(start, width);
            }
        }

        internal int SlotCount => Green.SlotCount;

        public bool IsList => Green.IsList;

        public bool IsMissing => Green.IsMissing;

        public bool IsToken => Green.IsToken;

        public bool IsTrivia => Green.IsTrivia;

        public bool HasLeadingTrivia
        {
            get
            {
                return GetLeadingTrivia().Count > 0;
            }
        }

        public bool HasTrailingTrivia
        {
            get
            {
                return GetTrailingTrivia().Count > 0;
            }
        }

        public bool ContainsDiagnostics => Green.ContainsDiagnostics;

        public bool ContainsAnnotations => Green.ContainsAnnotations;

        internal string SerializedValue => SyntaxSerializer.Serialize(this);

        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        public abstract void Accept(SyntaxVisitor visitor);

        internal abstract SyntaxNode GetNodeSlot(int index);

        internal abstract SyntaxNode GetCachedSlot(int index);

        internal SyntaxNode GetRed(ref SyntaxNode field, int slot)
        {
            var result = field;

            if (result == null)
            {
                var green = Green.GetSlot(slot);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, green.CreateRed(this, GetChildPosition(slot)), null);
                    result = field;
                }
            }

            return result;
        }

        // Special case of above function where slot = 0, does not need GetChildPosition 
        internal SyntaxNode GetRedAtZero(ref SyntaxNode field)
        {
            var result = field;

            if (result == null)
            {
                var green = Green.GetSlot(0);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, green.CreateRed(this, Position), null);
                    result = field;
                }
            }

            return result;
        }

        protected T GetRed<T>(ref T field, int slot) where T : SyntaxNode
        {
            var result = field;

            if (result == null)
            {
                var green = Green.GetSlot(slot);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, (T)green.CreateRed(this, this.GetChildPosition(slot)), null);
                    result = field;
                }
            }

            return result;
        }

        // special case of above function where slot = 0, does not need GetChildPosition 
        protected T GetRedAtZero<T>(ref T field) where T : SyntaxNode
        {
            var result = field;

            if (result == null)
            {
                var green = Green.GetSlot(0);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, (T)green.CreateRed(this, Position), null);
                    result = field;
                }
            }

            return result;
        }

        internal SyntaxNode GetRedElement(ref SyntaxNode element, int slot)
        {
            Debug.Assert(IsList);

            var result = element;

            if (result == null)
            {
                var green = Green.GetSlot(slot);
                // passing list's parent
                Interlocked.CompareExchange(ref element, green.CreateRed(Parent, GetChildPosition(slot)), null);
                result = element;
            }

            return result;
        }

        internal virtual int GetChildPosition(int index)
        {
            var offset = 0;
            var green = Green;
            while (index > 0)
            {
                index--;
                var prevSibling = GetCachedSlot(index);
                if (prevSibling != null)
                {
                    return prevSibling.EndPosition + offset;
                }
                var greenChild = green.GetSlot(index);
                if (greenChild != null)
                {
                    offset += greenChild.FullWidth;
                }
            }

            return Position + offset;
        }

        public virtual SyntaxTriviaList GetLeadingTrivia()
        {
            var firstToken = GetFirstToken();
            return firstToken != null ? firstToken.GetLeadingTrivia() : default(SyntaxTriviaList);
        }

        public virtual SyntaxTriviaList GetTrailingTrivia()
        {
            var lastToken = GetLastToken();
            return lastToken != null ? lastToken.GetTrailingTrivia() : default(SyntaxTriviaList);
        }

        internal SyntaxToken GetFirstToken()
        {
            return ((SyntaxToken)GetFirstTerminal());
        }

        internal SyntaxToken GetLastToken()
        {
            return ((SyntaxToken)GetLastTerminal());
        }

        public SyntaxNode GetFirstTerminal()
        {
            var node = this;

            do
            {
                var foundChild = false;
                for (int i = 0, n = node.SlotCount; i < n; i++)
                {
                    var child = node.GetNodeSlot(i);
                    if (child != null)
                    {
                        node = child;
                        foundChild = true;
                        break;
                    }
                }

                if (!foundChild)
                {
                    return null;
                }
            }
            while (node.SlotCount != 0);

            return node == this ? this : node;
        }

        public SyntaxNode GetLastTerminal()
        {
            var node = this;

            do
            {
                SyntaxNode lastChild = null;
                for (var i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetNodeSlot(i);
                    if (child != null && child.FullWidth > 0)
                    {
                        lastChild = child;
                        break;
                    }
                }
                node = lastChild;
            } while (node?.SlotCount > 0);

            return node;
        }

        /// <summary>
        /// The list of child nodes of this node, where each element is a SyntaxNode instance.
        /// </summary>
        public ChildSyntaxList ChildNodes()
        {
            return new ChildSyntaxList(this);
        }

        /// <summary>
        /// Gets a list of ancestor nodes
        /// </summary>
        public IEnumerable<SyntaxNode> Ancestors()
        {
            return Parent?
                .AncestorsAndSelf() ??
                Array.Empty<SyntaxNode>();
        }

        /// <summary>
        /// Gets a list of ancestor nodes (including this node) 
        /// </summary>
        public IEnumerable<SyntaxNode> AncestorsAndSelf()
        {
            for (var node = this; node != null; node = node.Parent)
            {
                yield return node;
            }
        }

        /// <summary>
        /// Gets the first node of type TNode that matches the predicate.
        /// </summary>
        public TNode FirstAncestorOrSelf<TNode>(Func<TNode, bool> predicate = null)
            where TNode : SyntaxNode
        {
            for (var node = this; node != null; node = node.Parent)
            {
                if (node is TNode tnode && (predicate == null || predicate(tnode)))
                {
                    return tnode;
                }
            }

            return default;
        }

        /// <summary>
        /// Gets a list of descendant nodes in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        public IEnumerable<SyntaxNode> DescendantNodes(Func<SyntaxNode, bool> descendIntoChildren = null)
        {
            return DescendantNodesImpl(FullSpan, descendIntoChildren, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes (including this node) in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null)
        {
            return DescendantNodesImpl(FullSpan, descendIntoChildren, includeSelf: true);
        }

        protected internal SyntaxNode ReplaceCore<TNode>(
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null)
            where TNode : SyntaxNode
        {
            return SyntaxReplacer.Replace(this, nodes, computeReplacementNode);
        }

        protected internal SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        {
            return SyntaxReplacer.ReplaceNodeInList(this, originalNode, replacementNodes);
        }

        protected internal SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return SyntaxReplacer.InsertNodeInList(this, nodeInList, nodesToInsert, insertBefore);
        }

        public RazorDiagnostic[] GetDiagnostics()
        {
            return Green.GetDiagnostics();
        }

        public SyntaxAnnotation[] GetAnnotations()
        {
            return Green.GetAnnotations();
        }

        public bool IsEquivalentTo(SyntaxNode other)
        {
            if (this == other)
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return Green.IsEquivalentTo(other.Green);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Green.ToString());
            builder.AppendFormat(" at {0}::{1}", Position, FullWidth);

            return builder.ToString();
        }

        public virtual string ToFullString()
        {
            return Green.ToFullString();
        }

        protected virtual string GetDebuggerDisplay()
        {
            if (IsToken)
            {
                return string.Format("{0};[{1}]", Kind, ToFullString());
            }

            return string.Format("{0} [{1}..{2})", Kind, Position, EndPosition);
        }
    }
}
