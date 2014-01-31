// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Editor
{
    // Manages edits to a span
    public class SpanEditHandler
    {
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : this(tokenizer, AcceptedCharacters.Any)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public SpanEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharacters accepted)
        {
            AcceptedCharacters = accepted;
            Tokenizer = tokenizer;
        }

        public AcceptedCharacters AcceptedCharacters { get; set; }

        /// <summary>
        /// Provides a set of hints to editors which may be manipulating the document in which this span is located.
        /// </summary>
        public EditorHints EditorHints { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public Func<string, IEnumerable<ISymbol>> Tokenizer { get; set; }

        public static SpanEditHandler CreateDefault()
        {
            return CreateDefault(s => Enumerable.Empty<ISymbol>());
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
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
            PartialParseResult result = PartialParseResult.Accepted;
            TextChange normalized = change.Normalize();
            if (!force)
            {
                result = CanAcceptChange(target, normalized);
            }

            // If the change is accepted then apply the change
            if (result.HasFlag(PartialParseResult.Accepted))
            {
                return new EditResult(result, UpdateSpan(target, normalized));
            }
            return new EditResult(result, new SpanBuilder(target));
        }

        public virtual bool OwnsChange(Span target, TextChange change)
        {
            int end = target.Start.AbsoluteIndex + target.Length;
            int changeOldEnd = change.OldPosition + change.OldLength;
            return change.OldPosition >= target.Start.AbsoluteIndex &&
                   (changeOldEnd < end || (changeOldEnd == end && AcceptedCharacters != AcceptedCharacters.None));
        }

        protected virtual PartialParseResult CanAcceptChange(Span target, TextChange normalizedChange)
        {
            return PartialParseResult.Rejected;
        }

        protected virtual SpanBuilder UpdateSpan(Span target, TextChange normalizedChange)
        {
            string newContent = normalizedChange.ApplyChange(target);
            SpanBuilder newSpan = new SpanBuilder(target);
            newSpan.ClearSymbols();
            foreach (ISymbol sym in Tokenizer(newContent))
            {
                sym.OffsetStart(target.Start);
                newSpan.Accept(sym);
            }
            if (target.Next != null)
            {
                SourceLocation newEnd = SourceLocationTracker.CalculateNewLocation(target.Start, newContent);
                target.Next.ChangeStart(newEnd);
            }
            return newSpan;
        }

        protected internal static bool IsAtEndOfFirstLine(Span target, TextChange change)
        {
            int endOfFirstLine = target.Content.IndexOfAny(new char[] { (char)0x000d, (char)0x000a, (char)0x2028, (char)0x2029 });
            return (endOfFirstLine == -1 || (change.OldPosition - target.Start.AbsoluteIndex) <= endOfFirstLine);
        }

        /// <summary>
        /// Returns true if the specified change is an insertion of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndInsertion(Span target, TextChange change)
        {
            return change.IsInsert && IsAtEndOfSpan(target, change);
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

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method should only be used on Spans")]
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

        // Is the specified span to the right of this span and immediately adjacent?
        internal static bool IsAdjacentOnRight(Span target, Span other)
        {
            return target.Start.AbsoluteIndex < other.Start.AbsoluteIndex && target.Start.AbsoluteIndex + target.Length == other.Start.AbsoluteIndex;
        }

        // Is the specified span to the left of this span and immediately adjacent?
        internal static bool IsAdjacentOnLeft(Span target, Span other)
        {
            return other.Start.AbsoluteIndex < target.Start.AbsoluteIndex && other.Start.AbsoluteIndex + other.Length == target.Start.AbsoluteIndex;
        }

        public override string ToString()
        {
            return GetType().Name + ";Accepts:" + AcceptedCharacters + ((EditorHints == EditorHints.None) ? String.Empty : (";Hints: " + EditorHints.ToString()));
        }

        public override bool Equals(object obj)
        {
            SpanEditHandler other = obj as SpanEditHandler;
            return other != null &&
                   AcceptedCharacters == other.AcceptedCharacters &&
                   EditorHints == other.EditorHints;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(AcceptedCharacters)
                .Add(EditorHints)
                .CombinedHash;
        }
    }
}
