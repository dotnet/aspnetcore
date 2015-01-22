// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class HtmlMarkupParser
    {
        public override void ParseDocument()
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
                    while (!EndOfFile)
                    {
                        SkipToAndParseCode(HtmlSymbolType.OpenAngle);
                        ScanTagInDocumentContext();
                    }
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);
                }
            }
        }

        /// <summary>
        /// Reads the content of a tag (if present) in the MarkupDocument (or MarkupSection) context,
        /// where we don't care about maintaining a stack of tags.
        /// </summary>
        private void ScanTagInDocumentContext()
        {
            if (At(HtmlSymbolType.OpenAngle))
            {
                if (NextIs(HtmlSymbolType.Bang))
                {
                    // Checking to see if we meet the conditions of a special '!' tag: <!DOCTYPE, <![CDATA[, <!--.
                    if (!IsBangEscape(lookahead: 1))
                    {
                        AcceptAndMoveNext(); // Accept '<'
                        BangTag();
                        return;
                    }

                    // We should behave like a normal tag that has a parser escape, fall through to the normal 
                    // tag logic.
                }
                else if (NextIs(HtmlSymbolType.QuestionMark))
                {
                    AcceptAndMoveNext(); // Accept '<'
                    XmlPI();
                    return;
                }

                Output(SpanKind.Markup);

                // Start tag block
                var tagBlock = Context.StartBlock(BlockType.Tag);

                AcceptAndMoveNext(); // Accept '<'

                if (!At(HtmlSymbolType.ForwardSlash))
                {
                    OptionalBangEscape();

                    // Parsing a start tag
                    var scriptTag = At(HtmlSymbolType.Text) &&
                                    string.Equals(CurrentSymbol.Content, "script", StringComparison.OrdinalIgnoreCase);
                    Optional(HtmlSymbolType.Text);
                    TagContent(); // Parse the tag, don't care about the content
                    Optional(HtmlSymbolType.ForwardSlash);
                    Optional(HtmlSymbolType.CloseAngle);

                    if (scriptTag)
                    {
                        Output(SpanKind.Markup);
                        tagBlock.Dispose();

                        SkipToEndScriptAndParseCode();
                        return;
                    }
                }
                else
                {
                    // Parsing an end tag
                    // This section can accept things like: '</p  >' or '</p>' etc.
                    Optional(HtmlSymbolType.ForwardSlash);

                    // Whitespace here is invalid (according to the spec)
                    OptionalBangEscape();
                    Optional(HtmlSymbolType.Text);
                    AcceptAll(HtmlSymbolType.WhiteSpace);
                    Optional(HtmlSymbolType.CloseAngle);
                }

                Output(SpanKind.Markup);

                // End tag block
                tagBlock.Dispose();
            }
        }
    }
}
