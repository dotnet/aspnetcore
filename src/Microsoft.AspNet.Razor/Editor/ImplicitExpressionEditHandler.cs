// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.Internal.Web.Utils;
using System;

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
            ImplicitExpressionEditHandler other = obj as ImplicitExpressionEditHandler;
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

            if (IsAcceptableReplace(target, normalizedChange))
            {
                return HandleReplacement(target, normalizedChange);
            }
            int changeRelativePosition = normalizedChange.OldPosition - target.Start.AbsoluteIndex;

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

            // Only support insertions at the end of the span
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

        private static bool IsAcceptableInsertion(Span target, TextChange change)
        {
            return IsEndInsertion(target, change) ||
                   (change.IsInsert && RemainingIsWhitespace(target, change));
        }

        private static bool RemainingIsWhitespace(Span target, TextChange change)
        {
            int offset = (change.OldPosition - target.Start.AbsoluteIndex) + change.OldLength;
            return String.IsNullOrWhiteSpace(target.Content.Substring(offset));
        }

        private PartialParseResult HandleReplacement(Span target, TextChange change)
        {
            // Special Case for IntelliSense commits.
            //  When IntelliSense commits, we get two changes (for example user typed "Date", then committed "DateTime" by pressing ".")
            //  1. Insert "." at the end of this span
            //  2. Replace the "Date." at the end of the span with "DateTime."
            //  We need partial parsing to accept case #2.
            string oldText = GetOldText(target, change);

            PartialParseResult result = PartialParseResult.Rejected;
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
                PartialParseResult result = PartialParseResult.Accepted;
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
            // If the insertion is a full identifier, accept it
            if (ParserHelpers.IsIdentifier(change.NewText))
            {
                return TryAcceptChange(target, change);
            }
            return PartialParseResult.Rejected;
        }

        private PartialParseResult TryAcceptChange(Span target, TextChange change, PartialParseResult acceptResult = PartialParseResult.Accepted)
        {
            string content = change.ApplyChange(target);
            if (StartsWithKeyword(content))
            {
                return PartialParseResult.Rejected | PartialParseResult.SpanContextChanged;
            }

            return acceptResult;
        }

        private bool StartsWithKeyword(string newContent)
        {
            using (StringReader reader = new StringReader(newContent))
            {
                return Keywords.Contains(reader.ReadWhile(ParserHelpers.IsIdentifierPart));
            }
        }
    }
}
