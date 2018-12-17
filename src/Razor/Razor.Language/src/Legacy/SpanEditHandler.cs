// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SpanEditHandler
    {
        private static readonly int TypeHashCode = typeof(SpanEditHandler).GetHashCode();

        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : this(tokenizer, AcceptedCharactersInternal.Any)
        {
        }

        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharactersInternal accepted)
        {
            AcceptedCharacters = accepted;
            Tokenizer = tokenizer;
        }

        public AcceptedCharactersInternal AcceptedCharacters { get; set; }

        public Func<string, IEnumerable<ISymbol>> Tokenizer { get; set; }

        public static SpanEditHandler CreateDefault(Func<string, IEnumerable<ISymbol>> tokenizer)
        {
            return new SpanEditHandler(tokenizer);
        }

        public virtual EditResult ApplyChange(Span target, SourceChange change)
        {
            return ApplyChange(target, change, force: false);
        }

        public virtual EditResult ApplyChange(Span target, SourceChange change, bool force)
        {
            var result = PartialParseResultInternal.Accepted;
            if (!force)
            {
                result = CanAcceptChange(target, change);
            }

            // If the change is accepted then apply the change
            if ((result & PartialParseResultInternal.Accepted) == PartialParseResultInternal.Accepted)
            {
                return new EditResult(result, UpdateSpan(target, change));
            }
            return new EditResult(result, new SpanBuilder(target));
        }

        public virtual bool OwnsChange(Span target, SourceChange change)
        {
            var end = target.Start.AbsoluteIndex + target.Length;
            var changeOldEnd = change.Span.AbsoluteIndex + change.Span.Length;
            return change.Span.AbsoluteIndex >= target.Start.AbsoluteIndex &&
                   (changeOldEnd < end || (changeOldEnd == end && AcceptedCharacters != AcceptedCharactersInternal.None));
        }

        protected virtual PartialParseResultInternal CanAcceptChange(Span target, SourceChange change)
        {
            return PartialParseResultInternal.Rejected;
        }

        protected virtual SpanBuilder UpdateSpan(Span target, SourceChange change)
        {
            var newContent = change.GetEditedContent(target);
            var newSpan = new SpanBuilder(target);
            newSpan.ClearSymbols();
            foreach (var token in Tokenizer(newContent))
            {
                newSpan.Accept(token);
            }
            if (target.Next != null)
            {
                var newEnd = SourceLocationTracker.CalculateNewLocation(target.Start, newContent);
                target.Next.ChangeStart(newEnd);
            }
            return newSpan;
        }

        protected internal static bool IsAtEndOfFirstLine(Span target, SourceChange change)
        {
            var endOfFirstLine = target.Content.IndexOfAny(new char[] { (char)0x000d, (char)0x000a, (char)0x2028, (char)0x2029 });
            return (endOfFirstLine == -1 || (change.Span.AbsoluteIndex - target.Start.AbsoluteIndex) <= endOfFirstLine);
        }

        /// <summary>
        /// Returns true if the specified change is an insertion of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndDeletion(Span target, SourceChange change)
        {
            return change.IsDelete && IsAtEndOfSpan(target, change);
        }

        /// <summary>
        /// Returns true if the specified change is a replacement of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndReplace(Span target, SourceChange change)
        {
            return change.IsReplace && IsAtEndOfSpan(target, change);
        }

        protected internal static bool IsAtEndOfSpan(Span target, SourceChange change)
        {
            return (change.Span.AbsoluteIndex + change.Span.Length) == (target.Start.AbsoluteIndex + target.Length);
        }

        public override string ToString()
        {
            return GetType().Name + ";Accepts:" + AcceptedCharacters;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SpanEditHandler;
            return other != null &&
                GetType() == other.GetType() &&
                AcceptedCharacters == other.AcceptedCharacters;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties but Equals also checks the type.
            return TypeHashCode;
        }
    }
}
