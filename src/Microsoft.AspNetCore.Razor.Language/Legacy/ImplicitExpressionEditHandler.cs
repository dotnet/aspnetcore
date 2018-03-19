// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
                AcceptTrailingDot == other.AcceptTrailingDot;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties and base has none.
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(AcceptTrailingDot);

            return hashCodeCombiner;
        }

        protected override PartialParseResultInternal CanAcceptChange(Span target, SourceChange change)
        {
            if (AcceptedCharacters == AcceptedCharactersInternal.Any)
            {
                return PartialParseResultInternal.Rejected;
            }

            // In some editors intellisense insertions are handled as "dotless commits".  If an intellisense selection is confirmed
            // via something like '.' a dotless commit will append a '.' and then insert the remaining intellisense selection prior
            // to the appended '.'.  This 'if' statement attempts to accept the intermediate steps of a dotless commit via
            // intellisense.  It will accept two cases:
            //     1. '@foo.' -> '@foobaz.'.
            //     2. '@foobaz..' -> '@foobaz.bar.'. Includes Sub-cases '@foobaz()..' -> '@foobaz().bar.' etc.
            // The key distinction being the double '.' in the second case.
            if (IsDotlessCommitInsertion(target, change))
            {
                return HandleDotlessCommitInsertion(target);
            }

            if (IsAcceptableIdentifierReplacement(target, change))
            {
                return TryAcceptChange(target, change);
            }

            if (IsAcceptableReplace(target, change))
            {
                return HandleReplacement(target, change);
            }
            var changeRelativePosition = change.Span.AbsoluteIndex - target.Start.AbsoluteIndex;

            // Get the edit context
            char? lastChar = null;
            if (changeRelativePosition > 0 && target.Content.Length > 0)
            {
                lastChar = target.Content[changeRelativePosition - 1];
            }

            // Don't support 0->1 length edits
            if (lastChar == null)
            {
                return PartialParseResultInternal.Rejected;
            }

            // Accepts cases when insertions are made at the end of a span or '.' is inserted within a span.
            if (IsAcceptableInsertion(target, change))
            {
                // Handle the insertion
                return HandleInsertion(target, lastChar.Value, change);
            }

            if (IsAcceptableInsertionInBalancedParenthesis(target, change))
            {
                return PartialParseResultInternal.Accepted;
            }

            if (IsAcceptableDeletion(target, change))
            {
                return HandleDeletion(target, lastChar.Value, change);
            }

            if (IsAcceptableDeletionInBalancedParenthesis(target, change))
            {
                return PartialParseResultInternal.Accepted;
            }

            return PartialParseResultInternal.Rejected;
        }

        // A dotless commit is the process of inserting a '.' with an intellisense selection.
        private static bool IsDotlessCommitInsertion(Span target, SourceChange change)
        {
            return IsNewDotlessCommitInsertion(target, change) || IsSecondaryDotlessCommitInsertion(target, change);
        }

        // Completing 'DateTime' in intellisense with a '.' could result in: '@DateT' -> '@DateT.' -> '@DateTime.' which is accepted.
        private static bool IsNewDotlessCommitInsertion(Span target, SourceChange change)
        {
            return !IsAtEndOfSpan(target, change) &&
                   change.Span.AbsoluteIndex > 0 &&
                   change.NewText.Length > 0 &&
                   target.Content.Last() == '.' &&
                   ParserHelpers.IsIdentifier(change.NewText, requireIdentifierStart: false) &&
                   (change.Span.Length == 0 || ParserHelpers.IsIdentifier(change.GetOriginalText(target), requireIdentifierStart: false));
        }

        // Once a dotless commit has been performed you then have something like '@DateTime.'.  This scenario is used to detect the
        // situation when you try to perform another dotless commit resulting in a textchange with '..'.  Completing 'DateTime.Now'
        // in intellisense with a '.' could result in: '@DateTime.' -> '@DateTime..' -> '@DateTime.Now.' which is accepted.
        private static bool IsSecondaryDotlessCommitInsertion(Span target, SourceChange change)
        {
            // Do not need to worry about other punctuation, just looking for double '.' (after change)
            return change.NewText.Length == 1 &&
                   change.NewText == "." &&
                   !string.IsNullOrEmpty(target.Content) &&
                   target.Content.Last() == '.' &&
                   change.Span.Length == 0;
        }

        private static bool IsAcceptableReplace(Span target, SourceChange change)
        {
            return IsEndReplace(target, change) ||
                   (change.IsReplace && RemainingIsWhitespace(target, change));
        }

        private bool IsAcceptableIdentifierReplacement(Span target, SourceChange change)
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

                // We're looking for the first symbol that contains the SourceChange.
                if (symbolEndIndex > change.Span.AbsoluteIndex)
                {
                    if (symbolEndIndex >= change.Span.AbsoluteIndex + change.Span.Length && symbol.Type == CSharpSymbolType.Identifier)
                    {
                        // The symbol we're changing happens to be an identifier. Need to check if its transformed state is also one.
                        // We do this transformation logic to capture the case that the new text change happens to not be an identifier;
                        // i.e. "5". Alone, it's numeric, within an identifier it's classified as identifier.
                        var transformedContent = change.GetEditedContent(symbol.Content, change.Span.AbsoluteIndex - symbolStartIndex);
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

        private static bool IsAcceptableDeletion(Span target, SourceChange change)
        {
            return IsEndDeletion(target, change) ||
                   (change.IsDelete && RemainingIsWhitespace(target, change));
        }

        // Acceptable insertions can occur at the end of a span or when a '.' is inserted within a span.
        private static bool IsAcceptableInsertion(Span target, SourceChange change)
        {
            return change.IsInsert &&
                (IsAcceptableEndInsertion(target, change) ||
                IsAcceptableInnerInsertion(target, change));
        }

        // Internal for testing
        internal static bool IsAcceptableDeletionInBalancedParenthesis(Span target, SourceChange change)
        {
            if (!change.IsDelete)
            {
                return false;
            }

            var changeStart = change.Span.AbsoluteIndex;
            var changeLength = change.Span.Length;
            var changeEnd = changeStart + changeLength;
            var tokens = target.Symbols.Cast<CSharpSymbol>().ToArray();
            if (!IsInsideParenthesis(changeStart, tokens) || !IsInsideParenthesis(changeEnd, tokens))
            {
                // Either the start or end of the delete does not fall inside of parenthesis, unacceptable inner deletion.
                return false;
            }

            var relativePosition = changeStart - target.Start.AbsoluteIndex;
            var deletionContent = target.Content.Substring(relativePosition, changeLength);

            if (deletionContent.IndexOfAny(new[] { '(', ')' }) >= 0)
            {
                // Change deleted some parenthesis
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool IsAcceptableInsertionInBalancedParenthesis(Span target, SourceChange change)
        {
            if (!change.IsInsert)
            {
                return false;
            }

            if (change.NewText.IndexOfAny(new[] { '(', ')' }) >= 0)
            {
                // Insertions of parenthesis aren't handled by us. If someone else wants to accept it, they can.
                return false;
            }

            var tokens = target.Symbols.Cast<CSharpSymbol>().ToArray();
            if (IsInsideParenthesis(change.Span.AbsoluteIndex, tokens))
            {
                return true;
            }

            return false;
        }

        // Internal for testing
        internal static bool IsInsideParenthesis(int position, IReadOnlyList<CSharpSymbol> tokens)
        {
            var balanceCount = 0;
            var foundInsertionPoint = false;
            for (var i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                if (ContainsPosition(position, currentToken))
                {
                    if (balanceCount == 0)
                    {
                        // Insertion point is outside of parenthesis, i.e. inserting at the pipe: @Foo|Baz()
                        return false;
                    }

                    foundInsertionPoint = true;
                }

                if (!TryUpdateBalanceCount(currentToken, ref balanceCount))
                {
                    // Couldn't update the count. This usually occurrs when we run into a ')' outside of any parenthesis.
                    return false;
                }

                if (foundInsertionPoint && balanceCount == 0)
                {
                    // Once parenthesis become balanced after the insertion point we return true, no need to go further.
                    // If they get unbalanced down the line the expression was already unbalanced to begin with and this
                    // change happens prior to any ambiguity.
                    return true;
                }
            }

            // Unbalanced parenthesis
            return false;
        }

        // Internal for testing
        internal static bool ContainsPosition(int position, CSharpSymbol currentToken)
        {
            var tokenStart = currentToken.Start.AbsoluteIndex;
            if (tokenStart == position)
            {
                // Token is exactly at the insertion point.
                return true;
            }

            var tokenEnd = tokenStart + currentToken.Content.Length;
            if (tokenStart < position && tokenEnd > position)
            {
                // Insertion point falls in the middle of the current token.
                return true;
            }

            return false;
        }

        // Internal for testing
        internal static bool TryUpdateBalanceCount(CSharpSymbol token, ref int count)
        {
            var updatedCount = count;
            if (token.Type == CSharpSymbolType.LeftParenthesis)
            {
                updatedCount++;
            }
            else if (token.Type == CSharpSymbolType.RightParenthesis)
            {
                if (updatedCount == 0)
                {
                    return false;
                }

                updatedCount--;
            }
            else if (token.Type == CSharpSymbolType.StringLiteral)
            {
                var content = token.Content;
                if (content.Length > 0 && content[content.Length - 1] != '"')
                {
                    // Incomplete string literal may have consumed some of our parenthesis and usually occurr during auto-completion of '"' => '""'.
                    if (!TryUpdateCountFromContent(content, ref updatedCount))
                    {
                        return false;
                    }
                }
            }
            else if (token.Type == CSharpSymbolType.CharacterLiteral)
            {
                var content = token.Content;
                if (content.Length > 0 && content[content.Length - 1] != '\'')
                {
                    // Incomplete character literal may have consumed some of our parenthesis and usually occurr during auto-completion of "'" => "''".
                    if (!TryUpdateCountFromContent(content, ref updatedCount))
                    {
                        return false;
                    }
                }
            }

            if (updatedCount < 0)
            {
                return false;
            }

            count = updatedCount;
            return true;
        }

        // Internal for testing
        internal static bool TryUpdateCountFromContent(string content, ref int count)
        {
            var updatedCount = count;
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == '(')
                {
                    updatedCount++;
                }
                else if (content[i] == ')')
                {
                    if (updatedCount == 0)
                    {
                        // Unbalanced parenthesis, i.e. @Foo)
                        return false;
                    }

                    updatedCount--;
                }
            }

            count = updatedCount;
            return true;
        }

        // Accepts character insertions at the end of spans.  AKA: '@foo' -> '@fooo' or '@foo' -> '@foo   ' etc.
        private static bool IsAcceptableEndInsertion(Span target, SourceChange change)
        {
            Debug.Assert(change.IsInsert);

            return IsAtEndOfSpan(target, change) ||
                   RemainingIsWhitespace(target, change);
        }

        // Accepts '.' insertions in the middle of spans. Ex: '@foo.baz.bar' -> '@foo..baz.bar'
        // This is meant to allow intellisense when editing a span.
        private static bool IsAcceptableInnerInsertion(Span target, SourceChange change)
        {
            Debug.Assert(change.IsInsert);

            // Ensure that we're actually inserting in the middle of a span and not at the end.
            // This case will fail if the IsAcceptableEndInsertion does not capture an end insertion correctly.
            Debug.Assert(!IsAtEndOfSpan(target, change));

            return change.Span.AbsoluteIndex > 0 &&
                   change.NewText == ".";
        }

        private static bool RemainingIsWhitespace(Span target, SourceChange change)
        {
            var offset = (change.Span.AbsoluteIndex - target.Start.AbsoluteIndex) + change.Span.Length;
            return string.IsNullOrWhiteSpace(target.Content.Substring(offset));
        }

        private PartialParseResultInternal HandleDotlessCommitInsertion(Span target)
        {
            var result = PartialParseResultInternal.Accepted;
            if (!AcceptTrailingDot && target.Content.LastOrDefault() == '.')
            {
                result |= PartialParseResultInternal.Provisional;
            }
            return result;
        }

        private PartialParseResultInternal HandleReplacement(Span target, SourceChange change)
        {
            // Special Case for IntelliSense commits.
            //  When IntelliSense commits, we get two changes (for example user typed "Date", then committed "DateTime" by pressing ".")
            //  1. Insert "." at the end of this span
            //  2. Replace the "Date." at the end of the span with "DateTime."
            //  We need partial parsing to accept case #2.
            var oldText = change.GetOriginalText(target);

            var result = PartialParseResultInternal.Rejected;
            if (EndsWithDot(oldText) && EndsWithDot(change.NewText))
            {
                result = PartialParseResultInternal.Accepted;
                if (!AcceptTrailingDot)
                {
                    result |= PartialParseResultInternal.Provisional;
                }
            }
            return result;
        }

        private PartialParseResultInternal HandleDeletion(Span target, char previousChar, SourceChange change)
        {
            // What's left after deleting?
            if (previousChar == '.')
            {
                return TryAcceptChange(target, change, PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional);
            }
            else if (ParserHelpers.IsIdentifierPart(previousChar))
            {
                return TryAcceptChange(target, change);
            }
            else if (previousChar == '(')
            {
                var changeRelativePosition = change.Span.AbsoluteIndex - target.Start.AbsoluteIndex;
                if (target.Content[changeRelativePosition] == ')')
                {
                    return PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional;
                }
            }

            return PartialParseResultInternal.Rejected;
        }

        private PartialParseResultInternal HandleInsertion(Span target, char previousChar, SourceChange change)
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
            else if (previousChar == '(')
            {
                return HandleInsertionAfterOpenParenthesis(target, change);
            }
            else
            {
                return PartialParseResultInternal.Rejected;
            }
        }

        private PartialParseResultInternal HandleInsertionAfterIdPart(Span target, SourceChange change)
        {
            // If the insertion is a full identifier part, accept it
            if (ParserHelpers.IsIdentifier(change.NewText, requireIdentifierStart: false))
            {
                return TryAcceptChange(target, change);
            }
            else if (IsDoubleParenthesisInsertion(change) || IsOpenParenthesisInsertion(change))
            {
                // Allow inserting parens after an identifier - this is needed to support signature
                // help intellisense in VS.
                return TryAcceptChange(target, change);
            }
            else if (EndsWithDot(change.NewText))
            {
                // Accept it, possibly provisionally
                var result = PartialParseResultInternal.Accepted;
                if (!AcceptTrailingDot)
                {
                    result |= PartialParseResultInternal.Provisional;
                }
                return TryAcceptChange(target, change, result);
            }
            else
            {
                return PartialParseResultInternal.Rejected;
            }
        }

        private PartialParseResultInternal HandleInsertionAfterOpenParenthesis(Span target, SourceChange change)
        {
            if (IsCloseParenthesisInsertion(change))
            {
                return TryAcceptChange(target, change);
            }

            return PartialParseResultInternal.Rejected;
        }

        private static bool IsDoubleParenthesisInsertion(SourceChange change)
        {
            return
                change.IsInsert &&
                change.NewText.Length == 2 &&
                change.NewText == "()";
        }

        private static bool IsOpenParenthesisInsertion(SourceChange change)
        {
            return
                change.IsInsert &&
                change.NewText.Length == 1 &&
                change.NewText == "(";
        }

        private static bool IsCloseParenthesisInsertion(SourceChange change)
        {
            return
                change.IsInsert &&
                change.NewText.Length == 1 &&
                change.NewText == ")";
        }

        private static bool EndsWithDot(string content)
        {
            return (content.Length == 1 && content[0] == '.') ||
                   (content[content.Length - 1] == '.' &&
                    content.Take(content.Length - 1).All(ParserHelpers.IsIdentifierPart));
        }

        private PartialParseResultInternal HandleInsertionAfterDot(Span target, SourceChange change)
        {
            // If the insertion is a full identifier or another dot, accept it
            if (ParserHelpers.IsIdentifier(change.NewText) || change.NewText == ".")
            {
                return TryAcceptChange(target, change);
            }
            return PartialParseResultInternal.Rejected;
        }

        private PartialParseResultInternal TryAcceptChange(Span target, SourceChange change, PartialParseResultInternal acceptResult = PartialParseResultInternal.Accepted)
        {
            var content = change.GetEditedContent(target);
            if (StartsWithKeyword(content))
            {
                return PartialParseResultInternal.Rejected | PartialParseResultInternal.SpanContextChanged;
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
