// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class SourceChange : IEquatable<SourceChange>
    {
        public SourceChange(int absoluteIndex, int length, string newText)
        {
            if (absoluteIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteIndex));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            Span = new SourceSpan(absoluteIndex, length);
            NewText = newText;
        }

        public SourceChange(SourceSpan span, string newText)
        {
            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            Span = span;
            NewText = newText;
        }

        public bool IsDelete => Span.Length > 0 && NewText.Length == 0;

        public bool IsInsert => Span.Length == 0 && NewText.Length > 0;

        public bool IsReplace => Span.Length > 0 && NewText.Length > 0;

        public SourceSpan Span { get; }

        public string NewText { get; }

        internal string GetEditedContent(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var offset = GetOffset(node);
            return GetEditedContent(node.GetContent(), offset);
        }

        internal string GetEditedContent(string text, int offset)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return text.Remove(offset, Span.Length).Insert(offset, NewText);
        }

        internal int GetOffset(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var start = Span.AbsoluteIndex;
            var end = Span.AbsoluteIndex + Span.Length;

            if (start < node.Position ||
                start > node.EndPosition ||
                end < node.Position ||
                end > node.EndPosition)
            {
                throw new InvalidOperationException(Resources.FormatInvalidOperation_SpanIsNotChangeOwner(node, this));
            }

            return start - node.Position;
        }

        internal string GetOriginalText(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.FullWidth == 0)
            {
                return string.Empty;
            }

            var offset = GetOffset(node);
            return node.GetContent().Substring(offset, Span.Length);
        }

        public bool Equals(SourceChange other)
        {
            return
                other != null &&
                Span.Equals(other.Span) &&
                string.Equals(NewText, other.NewText, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceChange);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(Span);
            hash.Add(NewText, StringComparer.Ordinal);
            return hash;
        }

        public override string ToString()
        {
            return Span.ToString() + " : " + NewText;
        }
    }
}
