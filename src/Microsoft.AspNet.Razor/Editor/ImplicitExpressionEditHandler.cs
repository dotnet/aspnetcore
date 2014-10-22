// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Editor
{
    public class ImplicitExpressionEditHandler : SpanEditHandler
    {
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public ImplicitExpressionEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, ISet<string> keywords, bool acceptTrailingDot)
            : base(tokenizer)
        {
            Initialize(keywords, acceptTrailingDot);
        }

        public bool AcceptTrailingDot { get; private set; }
        public ISet<string> Keywords { get; private set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0};ImplicitExpression[{1}];K{2}", base.ToString(), AcceptTrailingDot ? "ATD" : "RTD", Keywords.Count);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ImplicitExpressionEditHandler;
            return other != null &&
                   base.Equals(other) &&
                   Keywords.SetEquals(other.Keywords) &&
                   AcceptTrailingDot == other.AcceptTrailingDot;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(AcceptTrailingDot)
                .Add(Keywords)
                .CombinedHash;
        }

        protected override PartialParseResult CanAcceptChange(Span target, TextChange normalizedChange)
        {
            if (AcceptedCharacters == AcceptedCharacters.Any)
            {
                return PartialParseResult.Rejected;
            }

            // In some editors intellisense insertions are handled as "dotless commits".  If an intellisense selection is confirmed 
            // via something like '.' a dotless commit will append a '.' and then insert the remaining intellisense selection prior 
            // to the appended '.'.  This 'if' statement attempts to accept the intermediate steps of a dotless commit via 
            // intellisense.  It will accept two cases:
            //     1. '@foo.' -> '@foobaz.'.
            //     2. '@foobaz..' -> '@foobaz.bar.'. Includes Sub-cases '@foobaz()..' -> '@foobaz().bar.' etc.
            // The key distinction being the double '.' in the second case.
            if (IsDotlessCommitInsertion(target, normalizedChange))
            {
                return HandleDotlessCommitInsertion(target);
            }

            if (IsAcceptableReplace(target, normalizedChange))
            {
                return HandleReplacement(target, normalizedChange);
            }
            var changeRelativePosition = normalizedChange.OldPosition - target.Start.AbsoluteIndex;

            // Get the edit context
            char? lastChar = null;
            if (changeRelativePosition > 0 && target.Content.Length > 0)
            {
                lastChar = target.Content[changeRelativePosition - 1];
            }

            // Don't support 0->1 length edits
            if (lastChar == null)
            {
                return PartialParseResult.Rejected;
            }

            // Accepts cases when insertions are made at the end of a span or '.' is inserted within a span.
            if (IsAcceptableInsertion(target, normalizedChange))
            {
                // Handle the insertion
                return HandleInsertion(target, lastChar.Value, normalizedChange);
            }

            if (IsAcceptableDeletion(target, normalizedChange))
            {
                return HandleDeletion(target, lastChar.Value, normalizedChange);
            }

            return PartialParseResult.Rejected;
        }

        private void Initialize(ISet<string> keywords, bool acceptTrailingDot)
        {
            Keywords = keywords ?? new HashSet<string>();
            AcceptTrailingDot = acceptTrailingDot;
        }

        // A dotless commit is the process of inserting a '.' with an intellisense selection.
        private static bool IsDotlessCommitInsertion(Span target, TextChange change)
        {
            return IsNewDotlessCommitInsertion(target, change) || IsSecondaryDotlessCommitInsertion(target, change);
        }

        // Completing 'DateTime' in intellisense with a '.' could result in: '@DateT' -> '@DateT.' -> '@DateTime.' which is accepted.
        private static bool IsNewDotlessCommitInsertion(Span target, TextChange change)
        {
            return !IsAtEndOfSpan(target, change) &&
                   change.NewPosition > 0 &&
                   change.NewLength > 0 &&
                   target.Content.Last() == '.' &&
                   ParserHelpers.IsIdentifier(change.NewText, requireIdentifierStart: false) &&
                   (change.OldLength == 0 || ParserHelpers.IsIdentifier(change.OldText, requireIdentifierStart: false));
        }

        // Once a dotless commit has been performed you then have something like '@DateTime.'.  This scenario is used to detect the
        // situation when you try to perform another dotless commit resulting in a textchange with '..'.  Completing 'DateTime.Now' 
        // in intellisense with a '.' could result in: '@DateTime.' -> '@DateTime..' -> '@DateTime.Now.' which is accepted.
        private static bool IsSecondaryDotlessCommitInsertion(Span target, TextChange change)
        {
            // Do not need to worry about other punctuation, just looking for double '.' (after change)
            return change.NewLength == 1 &&
                   !String.IsNullOrEmpty(target.Content) &&
                   target.Content.Last() == '.' &&
                   change.NewText == "." &&
                   change.OldLength == 0;
        }

        private static bool IsAcceptableReplace(Span target, TextChange change)
        {
            return IsEndReplace(target, change) ||
                   (change.IsReplace && RemainingIsWhitespace(target, change));
        }

        private static bool IsAcceptableDeletion(Span target, TextChange change)
        {
            return IsEndDeletion(target, change) ||
                   (change.IsDelete && RemainingIsWhitespace(target, change));
        }

        // Acceptable insertions can occur at the end of a span or when a '.' is inserted within a span.
        private static bool IsAcceptableInsertion(Span target, TextChange change)
        {
            return change.IsInsert &&
                   (IsAcceptableEndInsertion(target, change) ||
                   IsAcceptableInnerInsertion(target, change));
        }

        // Accepts character insertions at the end of spans.  AKA: '@foo' -> '@fooo' or '@foo' -> '@foo   ' etc.
        private static bool IsAcceptableEndInsertion(Span target, TextChange change)
        {
            Debug.Assert(change.IsInsert);

            return IsAtEndOfSpan(target, change) ||
                   RemainingIsWhitespace(target, change);
        }

        // Accepts '.' insertions in the middle of spans. Ex: '@foo.baz.bar' -> '@foo..baz.bar'
        // This is meant to allow intellisense when editing a span.
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "target", Justification = "The 'target' parameter is used in Debug to validate that the function is called in the correct context.")]
        private static bool IsAcceptableInnerInsertion(Span target, TextChange change)
        {
            Debug.Assert(change.IsInsert);

            // Ensure that we're actually inserting in the middle of a span and not at the end.
            // This case will fail if the IsAcceptableEndInsertion does not capture an end insertion correctly.
            Debug.Assert(!IsAtEndOfSpan(target, change));

            return change.NewPosition > 0 &&
                   change.NewText == ".";
        }

        private static bool RemainingIsWhitespace(Span target, TextChange change)
        {
            var offset = (change.OldPosition - target.Start.AbsoluteIndex) + change.OldLength;
            return String.IsNullOrWhiteSpace(target.Content.Substring(offset));
        }

        private PartialParseResult HandleDotlessCommitInsertion(Span target)
        {
            var result = PartialParseResult.Accepted;
            if (!AcceptTrailingDot && target.Content.LastOrDefault() == '.')
            {
                result |= PartialParseResult.Provisional;
            }
            return result;
        }

        private PartialParseResult HandleReplacement(Span target, TextChange change)
        {
            // Special Case for IntelliSense commits.
            //  When IntelliSense commits, we get two changes (for example user typed "Date", then committed "DateTime" by pressing ".")
            //  1. Insert "." at the end of this span
            //  2. Replace the "Date." at the end of the span with "DateTime."
            //  We need partial parsing to accept case #2.
            var oldText = GetOldText(target, change);

            var result = PartialParseResult.Rejected;
            if (EndsWithDot(oldText) && EndsWithDot(change.NewText))
            {
                result = PartialParseResult.Accepted;
                if (!AcceptTrailingDot)
                {
                    result |= PartialParseResult.Provisional;
                }
            }
            return result;
        }

        private PartialParseResult HandleDeletion(Span target, char previousChar, TextChange change)
        {
            // What's left after deleting?
            if (previousChar == '.')
            {
                return TryAcceptChange(target, change, PartialParseResult.Accepted | PartialParseResult.Provisional);
            }
            else if (ParserHelpers.IsIdentifierPart(previousChar))
            {
                return TryAcceptChange(target, change);
            }
            else
            {
                return PartialParseResult.Rejected;
            }
        }

        private PartialParseResult HandleInsertion(Span target, char previousChar, TextChange change)
        {
            // What are we inserting after?
            if (previousChar == '.')
            {
                return HandleInsertionAfterDot(target, change);
            }
            else if (ParserHelpers.IsIdentifierPart(previousChar) || previousChar == ')' || previousChar == ']')
            {
                return HandleInsertionAfterIdPart(target, change);
            }
            else
            {
                return PartialParseResult.Rejected;
            }
        }

        private PartialParseResult HandleInsertionAfterIdPart(Span target, TextChange change)
        {
            // If the insertion is a full identifier part, accept it
            if (ParserHelpers.IsIdentifier(change.NewText, requireIdentifierStart: false))
            {
                return TryAcceptChange(target, change);
            }
            else if (EndsWithDot(change.NewText))
            {
                // Accept it, possibly provisionally
                var result = PartialParseResult.Accepted;
                if (!AcceptTrailingDot)
                {
                    result |= PartialParseResult.Provisional;
                }
                return TryAcceptChange(target, change, result);
            }
            else
            {
                return PartialParseResult.Rejected;
            }
        }

        private static bool EndsWithDot(string content)
        {
            return (content.Length == 1 && content[0] == '.') ||
                   (content[content.Length - 1] == '.' &&
                    content.Take(content.Length - 1).All(ParserHelpers.IsIdentifierPart));
        }

        private PartialParseResult HandleInsertionAfterDot(Span target, TextChange change)
        {
            // If the insertion is a full identifier or another dot, accept it
            if (ParserHelpers.IsIdentifier(change.NewText) || change.NewText == ".")
            {
                return TryAcceptChange(target, change);
            }
            return PartialParseResult.Rejected;
        }

        private PartialParseResult TryAcceptChange(Span target, TextChange change, PartialParseResult acceptResult = PartialParseResult.Accepted)
        {
            var content = change.ApplyChange(target);
            if (StartsWithKeyword(content))
            {
                return PartialParseResult.Rejected | PartialParseResult.SpanContextChanged;
            }

            return acceptResult;
        }

        private bool StartsWithKeyword(string newContent)
        {
            using (var reader = new StringReader(newContent))
            {
                return Keywords.Contains(reader.ReadWhile(ParserHelpers.IsIdentifierPart));
            }
        }
    }
}
