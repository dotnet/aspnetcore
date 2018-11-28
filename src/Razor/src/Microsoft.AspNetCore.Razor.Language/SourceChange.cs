// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;
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

        internal string GetEditedContent(Span span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            var offset = GetOffset(span);
            return GetEditedContent(span.Content, offset);
        }

        internal string GetEditedContent(string text, int offset)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return text.Remove(offset, Span.Length).Insert(offset, NewText);
        }

        internal int GetOffset(Span span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            var start = Span.AbsoluteIndex;
            var end = Span.AbsoluteIndex + Span.Length;

            if (start < span.Start.AbsoluteIndex ||
                start > span.Start.AbsoluteIndex + span.Length ||
                end < span.Start.AbsoluteIndex || 
                end > span.Start.AbsoluteIndex + span.Length)
            {
                throw new InvalidOperationException(Resources.FormatInvalidOperation_SpanIsNotChangeOwner(span, this));
            }

            return start - span.Start.AbsoluteIndex;
        }

        internal string GetOriginalText(Span span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            if (span.Length == 0)
            {
                return string.Empty;
            }

            var offset = GetOffset(span);
            return span.Content.Substring(offset, Span.Length);
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
