// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public partial class HtmlMarkupParser
    {
        private static readonly char[] ValidAfterTypeAttributeNameCharacters = { ' ', '\t', '\r', '\n', '\f', '=' };

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

                    // If the script tag expects javascript content then we should do minimal parsing until we reach
                    // the end script tag. Don't want to incorrectly parse a "var tag = '<input />';" as an HTML tag.
                    if (scriptTag && !CurrentScriptTagExpectsHtml())
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
                    Optional(HtmlSymbolType.WhiteSpace);
                    Optional(HtmlSymbolType.CloseAngle);
                }

                Output(SpanKind.Markup);

                // End tag block
                tagBlock.Dispose();
            }
        }

        private bool CurrentScriptTagExpectsHtml()
        {
            var blockBuilder = Context.CurrentBlock;

            Debug.Assert(blockBuilder != null);

            var typeAttribute = blockBuilder.Children
                .OfType<Block>()
                .Where(block =>
                    block.ChunkGenerator is AttributeBlockChunkGenerator &&
                    block.Children.Count() >= 2)
                .FirstOrDefault(IsTypeAttribute);

            if (typeAttribute != null)
            {
                var contentValues = typeAttribute.Children
                    .OfType<Span>()
                    .Where(childSpan => childSpan.ChunkGenerator is LiteralAttributeChunkGenerator)
                    .Select(childSpan => childSpan.Content);

                var scriptType = string.Concat(contentValues).Trim();

                // Does not allow charset parameter (or any other parameters).
                return string.Equals(scriptType, "text/html", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool IsTypeAttribute(Block block)
        {
            var span = block.Children.First() as Span;

            if (span == null)
            {
                return false;
            }

            var trimmedStartContent = span.Content.TrimStart();
            if (trimmedStartContent.StartsWith("type", StringComparison.OrdinalIgnoreCase) &&
                (trimmedStartContent.Length == 4 ||
                ValidAfterTypeAttributeNameCharacters.Contains(trimmedStartContent[4])))
            {
                return true;
            }

            return false;
        }
    }
}
