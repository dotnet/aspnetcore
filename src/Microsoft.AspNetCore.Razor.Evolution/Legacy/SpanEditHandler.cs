// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class SpanEditHandler
    {
        private static readonly int TypeHashCode = typeof(SpanEditHandler).GetHashCode();

        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : this(tokenizer, AcceptedCharacters.Any)
        {
        }

        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharacters accepted)
        {
            AcceptedCharacters = accepted;
            Tokenizer = tokenizer;
        }

        public AcceptedCharacters AcceptedCharacters { get; set; }

        public Func<string, IEnumerable<ISymbol>> Tokenizer { get; set; }

        public static SpanEditHandler CreateDefault(Func<string, IEnumerable<ISymbol>> tokenizer)
        {
            return new SpanEditHandler(tokenizer);
        }

        public virtual EditResult ApplyChange(Span target, TextChange change)
        {
            return ApplyChange(target, change, force: false);
        }

        public virtual EditResult ApplyChange(Span target, TextChange change, bool force)
        {
            var result = PartialParseResult.Accepted;
            var normalized = change.Normalize();
            if (!force)
            {
                result = CanAcceptChange(target, normalized);
            }

            // If the change is accepted then apply the change
            if ((result & PartialParseResult.Accepted) == PartialParseResult.Accepted)
            {
                return new EditResult(result, UpdateSpan(target, normalized));
            }
            return new EditResult(result, new SpanBuilder(target));
        }

        public virtual bool OwnsChange(Span target, TextChange change)
        {
            var end = target.Start.AbsoluteIndex + target.Length;
            var changeOldEnd = change.OldPosition + change.OldLength;
            return change.OldPosition >= target.Start.AbsoluteIndex &&
                   (changeOldEnd < end || (changeOldEnd == end && AcceptedCharacters != AcceptedCharacters.None));
        }

        protected virtual PartialParseResult CanAcceptChange(Span target, TextChange normalizedChange)
        {
            return PartialParseResult.Rejected;
        }

        protected virtual SpanBuilder UpdateSpan(Span target, TextChange normalizedChange)
        {
            var newContent = normalizedChange.ApplyChange(target);
            var newSpan = new SpanBuilder(target);
            newSpan.ClearSymbols();
            foreach (ISymbol sym in Tokenizer(newContent))
            {
                newSpan.Accept(sym);
            }
            if (target.Next != null)
            {
                var newEnd = SourceLocationTracker.CalculateNewLocation(target.Start, newContent);
                target.Next.ChangeStart(newEnd);
            }
            return newSpan;
        }

        protected internal static bool IsAtEndOfFirstLine(Span target, TextChange change)
        {
            var endOfFirstLine = target.Content.IndexOfAny(new char[] { (char)0x000d, (char)0x000a, (char)0x2028, (char)0x2029 });
            return (endOfFirstLine == -1 || (change.OldPosition - target.Start.AbsoluteIndex) <= endOfFirstLine);
        }

        /// <summary>
        /// Returns true if the specified change is an insertion of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndDeletion(Span target, TextChange change)
        {
            return change.IsDelete && IsAtEndOfSpan(target, change);
        }

        /// <summary>
        /// Returns true if the specified change is a replacement of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndReplace(Span target, TextChange change)
        {
            return change.IsReplace && IsAtEndOfSpan(target, change);
        }

        protected internal static bool IsAtEndOfSpan(Span target, TextChange change)
        {
            return (change.OldPosition + change.OldLength) == (target.Start.AbsoluteIndex + target.Length);
        }

        /// <summary>
        /// Returns the old text referenced by the change.
        /// </summary>
        /// <remarks>
        /// If the content has already been updated by applying the change, this data will be _invalid_
        /// </remarks>
        protected internal static string GetOldText(Span target, TextChange change)
        {
            return target.Content.Substring(change.OldPosition - target.Start.AbsoluteIndex, change.OldLength);
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
