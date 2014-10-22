// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
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

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            span.Kind = SpanKind.Markup;
            span.CodeGenerator = new MarkupCodeGenerator();
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
                if (At(HtmlSymbolType.NewLine))
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
                        Span.CodeGenerator = SpanCodeGenerator.Null;
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
                        Accept(last);
                        last = null;
                    }
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);
                    RazorComment();
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
    }
}
