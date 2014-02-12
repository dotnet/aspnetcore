// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class HtmlMarkupParser
    {
        private bool CaseSensitive { get; set; }

        private StringComparison Comparison
        {
            get { return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        public override void ParseSection(Tuple<string, string> nestingSequences, bool caseSensitive)
        {
            if (Context == null)
            {
                throw new InvalidOperationException(RazorResources.Parser_Context_Not_Set);
            }

            using (PushSpanConfig(DefaultMarkupSpan))
            {
                using (Context.StartBlock(BlockType.Markup))
                {
                    NextToken();
                    CaseSensitive = caseSensitive;
                    if (nestingSequences.Item1 == null)
                    {
                        NonNestingSection(nestingSequences.Item2.Split());
                    }
                    else
                    {
                        NestingSection(nestingSequences);
                    }
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);
                }
            }
        }

        private void NonNestingSection(string[] nestingSequenceComponents)
        {
            do
            {
                SkipToAndParseCode(sym => sym.Type == HtmlSymbolType.OpenAngle || AtEnd(nestingSequenceComponents));
                ScanTagInDocumentContext();
                if (!EndOfFile && AtEnd(nestingSequenceComponents))
                {
                    break;
                }
            }
            while (!EndOfFile);

            PutCurrentBack();
        }

        private void NestingSection(Tuple<string, string> nestingSequences)
        {
            int nesting = 1;
            while (nesting > 0 && !EndOfFile)
            {
                SkipToAndParseCode(sym =>
                    sym.Type == HtmlSymbolType.Text ||
                    sym.Type == HtmlSymbolType.OpenAngle);
                if (At(HtmlSymbolType.Text))
                {
                    nesting += ProcessTextToken(nestingSequences, nesting);
                    if (CurrentSymbol != null)
                    {
                        AcceptAndMoveNext();
                    }
                    else if (nesting > 0)
                    {
                        NextToken();
                    }
                }
                else
                {
                    ScanTagInDocumentContext();
                }
            }
        }

        private bool AtEnd(string[] nestingSequenceComponents)
        {
            EnsureCurrent();
            if (String.Equals(CurrentSymbol.Content, nestingSequenceComponents[0], Comparison))
            {
                int bookmark = CurrentSymbol.Start.AbsoluteIndex;
                try
                {
                    foreach (string component in nestingSequenceComponents)
                    {
                        if (!EndOfFile && !String.Equals(CurrentSymbol.Content, component, Comparison))
                        {
                            return false;
                        }
                        NextToken();
                        while (!EndOfFile && IsSpacingToken(includeNewLines: true)(CurrentSymbol))
                        {
                            NextToken();
                        }
                    }
                    return true;
                }
                finally
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                }
            }
            return false;
        }

        private int ProcessTextToken(Tuple<string, string> nestingSequences, int currentNesting)
        {
            for (int i = 0; i < CurrentSymbol.Content.Length; i++)
            {
                int nestingDelta = HandleNestingSequence(nestingSequences.Item1, i, currentNesting, 1);
                if (nestingDelta == 0)
                {
                    nestingDelta = HandleNestingSequence(nestingSequences.Item2, i, currentNesting, -1);
                }

                if (nestingDelta != 0)
                {
                    return nestingDelta;
                }
            }
            return 0;
        }

        private int HandleNestingSequence(string sequence, int position, int currentNesting, int retIfMatched)
        {
            if (sequence != null &&
                CurrentSymbol.Content[position] == sequence[0] &&
                position + sequence.Length <= CurrentSymbol.Content.Length)
            {
                string possibleStart = CurrentSymbol.Content.Substring(position, sequence.Length);
                if (String.Equals(possibleStart, sequence, Comparison))
                {
                    // Capture the current symbol and "put it back" (really we just want to clear CurrentSymbol)
                    int bookmark = Context.Source.Position;
                    HtmlSymbol sym = CurrentSymbol;
                    PutCurrentBack();

                    // Carve up the symbol
                    Tuple<HtmlSymbol, HtmlSymbol> pair = Language.SplitSymbol(sym, position, HtmlSymbolType.Text);
                    HtmlSymbol preSequence = pair.Item1;
                    Debug.Assert(pair.Item2 != null);
                    pair = Language.SplitSymbol(pair.Item2, sequence.Length, HtmlSymbolType.Text);
                    HtmlSymbol sequenceToken = pair.Item1;
                    HtmlSymbol postSequence = pair.Item2;

                    // Accept the first chunk (up to the nesting sequence we just saw)
                    if (!String.IsNullOrEmpty(preSequence.Content))
                    {
                        Accept(preSequence);
                    }

                    if (currentNesting + retIfMatched == 0)
                    {
                        // This is 'popping' the final entry on the stack of nesting sequences
                        // A caller higher in the parsing stack will accept the sequence token, so advance
                        // to it
                        Context.Source.Position = sequenceToken.Start.AbsoluteIndex;
                    }
                    else
                    {
                        // This isn't the end of the last nesting sequence, accept the token and keep going
                        Accept(sequenceToken);

                        // Position at the start of the postSequence symbol
                        if (postSequence != null)
                        {
                            Context.Source.Position = postSequence.Start.AbsoluteIndex;
                        }
                        else
                        {
                            Context.Source.Position = bookmark;
                        }
                    }

                    // Return the value we were asked to return if matched, since we found a nesting sequence
                    return retIfMatched;
                }
            }
            return 0;
        }
    }
}
