// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
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
        /// <returns>A boolean indicating if we scanned at least one tag.</returns>
        private bool ScanTagInDocumentContext()
        {
            if (Optional(HtmlSymbolType.OpenAngle))
            {
                if (At(HtmlSymbolType.Bang))
                {
                    BangTag();
                    return true;
                }
                else if (At(HtmlSymbolType.QuestionMark))
                {
                    XmlPI();
                    return true;
                }
                else if (!At(HtmlSymbolType.Solidus))
                {
                    bool scriptTag = At(HtmlSymbolType.Text) &&
                                     String.Equals(CurrentSymbol.Content, "script", StringComparison.OrdinalIgnoreCase);
                    Optional(HtmlSymbolType.Text);
                    TagContent(); // Parse the tag, don't care about the content
                    Optional(HtmlSymbolType.Solidus);
                    Optional(HtmlSymbolType.CloseAngle);
                    if (scriptTag)
                    {
                        SkipToEndScriptAndParseCode();
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
