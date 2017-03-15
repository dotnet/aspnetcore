// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class ImplicitExpressionEditHandler : SpanEditHandler
    {
        private readonly ISet<string> _keywords;
        private readonly IReadOnlyCollection<string> _readOnlyKeywords;

        public ImplicitExpressionEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, ISet<string> keywords, bool acceptTrailingDot)
            : base(tokenizer)
        {
            _keywords = keywords ?? new HashSet<string>();

            // HashSet<T> implements IReadOnlyCollection<T> as of 4.6, but does not for 4.5.1. If the runtime cast
            // succeeds, avoid creating a new collection.
            _readOnlyKeywords = (_keywords as IReadOnlyCollection<string>) ?? _keywords.ToArray();

            AcceptTrailingDot = acceptTrailingDot;
        }

        public bool AcceptTrailingDot { get; }

        public IReadOnlyCollection<string> Keywords
        {
            get
            {
                return _readOnlyKeywords;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0};ImplicitExpression[{1}];K{2}", base.ToString(), AcceptTrailingDot ? "ATD" : "RTD", Keywords.Count);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ImplicitExpressionEditHandler;
            return base.Equals(other) &&
                _keywords.SetEquals(other._keywords) &&
                AcceptTrailingDot == other.AcceptTrailingDot;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties and base has none.
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Keywords);
            hashCodeCombiner.Add(AcceptTrailingDot);

            return hashCodeCombiner;
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

            if (IsAcceptableIdentifierReplacement(target, normalizedChange))
            {
                return TryAcceptChange(target, normalizedChange);
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
                   !string.IsNullOrEmpty(target.Content) &&
                   target.Content.Last() == '.' &&
                   change.NewText == "." &&
                   change.OldLength == 0;
        }

        private static bool IsAcceptableReplace(Span target, TextChange change)
        {
            return IsEndReplace(target, change) ||
                   (change.IsReplace && RemainingIsWhitespace(target, change));
        }

        private bool IsAcceptableIdentifierReplacement(Span target, TextChange change)
        {
            if (!change.IsReplace)
            {
                return false;
            }

            for (var i = 0; i < target.Symbols.Count; i++)
            {
                var symbol = target.Symbols[i] as CSharpSymbol;

                if (symbol == null)
                {
                    break;
                }

                var symbolStartIndex = symbol.Start.AbsoluteIndex;
                var symbolEndIndex = symbolStartIndex + symbol.Content.Length;

                // We're looking for the first symbol that contains the TextChange.
                if (symbolEndIndex > change.OldPosition)
                {
                    if (symbolEndIndex >= change.OldPosition + change.OldLength && symbol.Type == CSharpSymbolType.Identifier)
                    {
                        // The symbol we're changing happens to be an identifier. Need to check if its transformed state is also one.
                        // We do this transformation logic to capture the case that the new text change happens to not be an identifier;
                        // i.e. "5". Alone, it's numeric, within an identifier it's classified as identifier.
                        var transformedContent = change.ApplyChange(symbol.Content, symbolStartIndex);
                        var newSymbols = Tokenizer(transformedContent);

                        if (newSymbols.Count() != 1)
                        {
                            // The transformed content resulted in more than one symbol; we can only replace a single identifier with
                            // another single identifier.
                            break;
                        }

                        var newSymbol = (CSharpSymbol)newSymbols.First();
                        if (newSymbol.Type == CSharpSymbolType.Identifier)
                        {
                            return true;
                        }
                    }

                    // Change is touching a non-identifier symbol or spans multiple symbols.

                    break;
                }
            }

            return false;
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
            return string.IsNullOrWhiteSpace(target.Content.Substring(offset));
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
            var content = change.ApplyChange(target.Content, target.Start.AbsoluteIndex);
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
                return _keywords.Contains(reader.ReadWhile(ParserHelpers.IsIdentifierPart));
            }
        }
    }
}
