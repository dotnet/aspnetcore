// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
