// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;

namespace Microsoft.AspNetCore.Razor.Parser.Internal
{
    public partial class HtmlMarkupParser : TokenizerBackedParser<HtmlTokenizer, HtmlSymbol, HtmlSymbolType>
    {
        //From http://dev.w3.org/html5/spec/Overview.html#elements-0
        private ISet<string> _voidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area",
            "base",
            "br",
            "col",
            "command",
            "embed",
            "hr",
            "img",
            "input",
            "keygen",
            "link",
            "meta",
            "param",
            "source",
            "track",
            "wbr"
        };

        public ISet<string> VoidElements
        {
            get { return _voidElements; }
        }

        protected override ParserBase OtherParser
        {
            get { return Context.CodeParser; }
        }

        protected override LanguageCharacteristics<HtmlTokenizer, HtmlSymbol, HtmlSymbolType> Language
        {
            get { return HtmlLanguageCharacteristics.Instance; }
        }

        protected override bool SymbolTypeEquals(HtmlSymbolType x, HtmlSymbolType y) => x == y;

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            span.Kind = SpanKind.Markup;
            span.ChunkGenerator = new MarkupChunkGenerator();
            base.BuildSpan(span, start, content);
        }

        protected override void OutputSpanBeforeRazorComment()
        {
            Output(SpanKind.Markup);
        }

        protected void SkipToAndParseCode(HtmlSymbolType type)
        {
            SkipToAndParseCode(sym => sym.Type == type);
        }

        protected void SkipToAndParseCode(Func<HtmlSymbol, bool> condition)
        {
            HtmlSymbol last = null;
            var startOfLine = false;
            while (!EndOfFile && !condition(CurrentSymbol))
            {
                if (Context.NullGenerateWhitespaceAndNewLine)
                {
                    Context.NullGenerateWhitespaceAndNewLine = false;
                    Span.ChunkGenerator = SpanChunkGenerator.Null;
                    AcceptWhile(symbol => symbol.Type == HtmlSymbolType.WhiteSpace);
                    if (At(HtmlSymbolType.NewLine))
                    {
                        AcceptAndMoveNext();
                    }

                    Output(SpanKind.Markup);
                }
                else if (At(HtmlSymbolType.NewLine))
                {
                    if (last != null)
                    {
                        Accept(last);
                    }

                    // Mark the start of a new line
                    startOfLine = true;
                    last = null;
                    AcceptAndMoveNext();
                }
                else if (At(HtmlSymbolType.Transition))
                {
                    var transition = CurrentSymbol;
                    NextToken();
                    if (At(HtmlSymbolType.Transition))
                    {
                        if (last != null)
                        {
                            Accept(last);
                            last = null;
                        }
                        Output(SpanKind.Markup);
                        Accept(transition);
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKind.Markup);
                        AcceptAndMoveNext();
                        continue; // while
                    }
                    else
                    {
                        if (!EndOfFile)
                        {
                            PutCurrentBack();
                        }
                        PutBack(transition);
                    }

                    // Handle whitespace rewriting
                    if (last != null)
                    {
                        if (!Context.DesignTimeMode && last.Type == HtmlSymbolType.WhiteSpace && startOfLine)
                        {
                            // Put the whitespace back too
                            startOfLine = false;
                            PutBack(last);
                            last = null;
                        }
                        else
                        {
                            // Accept last
                            Accept(last);
                            last = null;
                        }
                    }

                    OtherParserBlock();
                }
                else if (At(HtmlSymbolType.RazorCommentTransition))
                {
                    if (last != null)
                    {
                        // Don't render the whitespace between the start of the line and the razor comment.
                        if (startOfLine && last.Type == HtmlSymbolType.WhiteSpace)
                        {
                            AddMarkerSymbolIfNecessary();
                            // Output the symbols that may have been accepted prior to the whitespace.
                            Output(SpanKind.Markup);

                            Span.ChunkGenerator = SpanChunkGenerator.Null;
                        }

                        Accept(last);
                        last = null;
                    }

                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);

                    RazorComment();

                    // Handle the whitespace and newline at the end of a razor comment.
                    if (startOfLine &&
                        (At(HtmlSymbolType.NewLine) ||
                        (At(HtmlSymbolType.WhiteSpace) && NextIs(HtmlSymbolType.NewLine))))
                    {
                        AcceptWhile(IsSpacingToken(includeNewLines: false));
                        AcceptAndMoveNext();
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKind.Markup);
                    }
                }
                else
                {
                    // As long as we see whitespace, we're still at the "start" of the line
                    startOfLine &= At(HtmlSymbolType.WhiteSpace);

                    // If there's a last token, accept it
                    if (last != null)
                    {
                        Accept(last);
                        last = null;
                    }

                    // Advance
                    last = CurrentSymbol;
                    NextToken();
                }
            }

            if (last != null)
            {
                Accept(last);
            }
        }

        protected static Func<HtmlSymbol, bool> IsSpacingToken(bool includeNewLines)
        {
            return sym => sym.Type == HtmlSymbolType.WhiteSpace || (includeNewLines && sym.Type == HtmlSymbolType.NewLine);
        }

        private void OtherParserBlock()
        {
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.Markup);
            using (PushSpanConfig())
            {
                Context.SwitchActiveParser();
                Context.CodeParser.ParseBlock();
                Context.SwitchActiveParser();
            }
            Initialize(Span);
            NextToken();
        }

        private bool IsBangEscape(int lookahead)
        {
            var potentialBang = Lookahead(lookahead);

            if (potentialBang != null &&
                potentialBang.Type == HtmlSymbolType.Bang)
            {
                var afterBang = Lookahead(lookahead + 1);

                return afterBang != null &&
                    afterBang.Type == HtmlSymbolType.Text &&
                    !string.Equals(afterBang.Content, "DOCTYPE", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void OptionalBangEscape()
        {
            if (IsBangEscape(lookahead: 0))
            {
                Output(SpanKind.Markup);

                // Accept the parser escape character '!'.
                Assert(HtmlSymbolType.Bang);
                AcceptAndMoveNext();

                // Setup the metacode span that we will be outputing.
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKind.MetaCode, AcceptedCharacters.None);
            }
        }
    }
}
