// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract class SyntaxNode
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

        internal abstract SyntaxNode Accept(SyntaxVisitor visitor);

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
                for (var i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetNodeSlot(i);
                    if (child != null)
                    {
                        node = child;
                        break;
                    }
                }
            } while (node.SlotCount != 0);

            return node == this ? this : node;
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
            return Green.ToString();
        }

        public virtual string ToFullString()
        {
            return Green.ToFullString();
        }
    }
}
