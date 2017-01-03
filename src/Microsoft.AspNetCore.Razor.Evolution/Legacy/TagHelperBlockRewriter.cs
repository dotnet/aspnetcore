// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal static class TagHelperBlockRewriter
    {
        private static readonly string StringTypeName = typeof(string).FullName;

        public static TagHelperBlockBuilder Rewrite(
            string tagName,
            bool validStructure,
            Block tag,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink)
        {
            // There will always be at least one child for the '<'.
            var start = tag.Children.First().Start;
            var attributes = GetTagAttributes(tagName, validStructure, tag, descriptors, errorSink);
            var tagMode = GetTagMode(tagName, tag, descriptors, errorSink);

            return new TagHelperBlockBuilder(tagName, tagMode, start, attributes, descriptors);
        }

        private static IList<TagHelperAttributeNode> GetTagAttributes(
            string tagName,
            bool validStructure,
            Block tagBlock,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink)
        {
            // Ignore all but one descriptor per type since this method uses the TagHelperDescriptors only to get the
            // contained TagHelperAttributeDescriptor's.
            descriptors = descriptors.Distinct(TypeBasedTagHelperDescriptorComparer.Default);

            var attributes = new List<TagHelperAttributeNode>();

            // We skip the first child "<tagname" and take everything up to the ending portion of the tag ">" or "/>".
            // The -2 accounts for both the start and end tags. If the tag does not have a valid structure then there's
            // no end tag to ignore.
            var symbolOffset = validStructure ? 2 : 1;
            var attributeChildren = tagBlock.Children.Skip(1).Take(tagBlock.Children.Count() - symbolOffset);

            foreach (var child in attributeChildren)
            {
                TryParseResult result;
                if (child.IsBlock)
                {
                    result = TryParseBlock(tagName, (Block)child, descriptors, errorSink);
                }
                else
                {
                    result = TryParseSpan((Span)child, descriptors, errorSink);
                }

                // Only want to track the attribute if we succeeded in parsing its corresponding Block/Span.
                if (result != null)
                {
                    SourceLocation? errorLocation = null;

                    // Check if it's a bound attribute that is minimized or if it's a bound non-string attribute that
                    // is null or whitespace.
                    if ((result.IsBoundAttribute && result.AttributeValueNode == null) ||
                        (result.IsBoundNonStringAttribute &&
                         IsNullOrWhitespaceAttributeValue(result.AttributeValueNode)))
                    {
                        errorLocation = GetAttributeNameStartLocation(child);

                        errorSink.OnError(
                            errorLocation.Value,
                            LegacyResources.FormatRewriterError_EmptyTagHelperBoundAttribute(
                                result.AttributeName,
                                tagName,
                                GetPropertyType(result.AttributeName, descriptors)),
                            result.AttributeName.Length);
                    }

                    // Check if the attribute was a prefix match for a tag helper dictionary property but the
                    // dictionary key would be the empty string.
                    if (result.IsMissingDictionaryKey)
                    {
                        if (!errorLocation.HasValue)
                        {
                            errorLocation = GetAttributeNameStartLocation(child);
                        }

                        errorSink.OnError(
                            errorLocation.Value,
                            LegacyResources.FormatTagHelperBlockRewriter_IndexerAttributeNameMustIncludeKey(
                                result.AttributeName,
                                tagName),
                            result.AttributeName.Length);
                    }

                    var attributeNode = new TagHelperAttributeNode(
                        result.AttributeName,
                        result.AttributeValueNode,
                        result.AttributeValueStyle);

                    attributes.Add(attributeNode);
                }
                else
                {
                    // Error occured while parsing the attribute. Don't try parsing the rest to avoid misleading errors.
                    break;
                }
            }

            return attributes;
        }

        private static TagMode GetTagMode(
            string tagName,
            Block beginTagBlock,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink)
        {
            var childSpan = beginTagBlock.FindLastDescendentSpan();

            // Self-closing tags are always valid despite descriptors[X].TagStructure.
            if (childSpan?.Content.EndsWith("/>", StringComparison.Ordinal) ?? false)
            {
                return TagMode.SelfClosing;
            }

            var baseDescriptor = descriptors.FirstOrDefault(
                descriptor => descriptor.TagStructure != TagStructure.Unspecified);
            var resolvedTagStructure = baseDescriptor?.TagStructure ?? TagStructure.Unspecified;
            if (resolvedTagStructure == TagStructure.WithoutEndTag)
            {
                return TagMode.StartTagOnly;
            }

            return TagMode.StartTagAndEndTag;
        }

        // This method handles cases when the attribute is a simple span attribute such as
        // class="something moresomething".  This does not handle complex attributes such as
        // class="@myclass". Therefore the span.Content is equivalent to the entire attribute.
        private static TryParseResult TryParseSpan(
            Span span,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink)
        {
            var afterEquals = false;
            var builder = new SpanBuilder(span.Start)
            {
                ChunkGenerator = span.ChunkGenerator,
                EditHandler = span.EditHandler,
                Kind = span.Kind
            };

            // Will contain symbols that represent a single attribute value: <input| class="btn"| />
            var htmlSymbols = span.Symbols.OfType<HtmlSymbol>().ToArray();
            var capturedAttributeValueStart = false;
            var attributeValueStartLocation = span.Start;

            // Default to DoubleQuotes. We purposefully do not persist NoQuotes ValueStyle to stay consistent with the
            // TryParseBlock() variation of attribute parsing.
            var attributeValueStyle = HtmlAttributeValueStyle.DoubleQuotes;

            // The symbolOffset is initialized to 0 to expect worst case: "class=". If a quote is found later on for
            // the attribute value the symbolOffset is adjusted accordingly.
            var symbolOffset = 0;
            string name = null;

            // Iterate down through the symbols to find the name and the start of the value.
            // We subtract the symbolOffset so we don't accept an ending quote of a span.
            for (var i = 0; i < htmlSymbols.Length - symbolOffset; i++)
            {
                var symbol = htmlSymbols[i];

                if (afterEquals)
                {
                    // We've captured all leading whitespace, the attribute name, and an equals with an optional
                    // quote/double quote. We're now at: " asp-for='|...'" or " asp-for=|..."
                    // The goal here is to capture all symbols until the end of the attribute. Note this will not
                    // consume an ending quote due to the symbolOffset.

                    // When symbols are accepted into SpanBuilders, their locations get altered to be offset by the
                    // parent which is why we need to mark our start location prior to adding the symbol.
                    // This is needed to know the location of the attribute value start within the document.
                    if (!capturedAttributeValueStart)
                    {
                        capturedAttributeValueStart = true;

                        attributeValueStartLocation = symbol.Start;
                    }

                    builder.Accept(symbol);
                }
                else if (name == null && HtmlMarkupParser.IsValidAttributeNameSymbol(symbol))
                {
                    // We've captured all leading whitespace prior to the attribute name.
                    // We're now at: " |asp-for='...'" or " |asp-for=..."
                    // The goal here is to capture the attribute name.

                    var nameBuilder = new StringBuilder();
                    // Move the indexer past the attribute name symbols.
                    for (var j = i; j < htmlSymbols.Length; j++)
                    {
                        var nameSymbol = htmlSymbols[j];
                        if (!HtmlMarkupParser.IsValidAttributeNameSymbol(nameSymbol))
                        {
                            break;
                        }

                        nameBuilder.Append(nameSymbol.Content);
                        i++;
                    }

                    i--;

                    name = nameBuilder.ToString();
                    attributeValueStartLocation = SourceLocationTracker.Advance(attributeValueStartLocation, name);
                }
                else if (symbol.Type == HtmlSymbolType.Equals)
                {
                    // We've captured all leading whitespace and the attribute name.
                    // We're now at: " asp-for|='...'" or " asp-for|=..."
                    // The goal here is to consume the equal sign and the optional single/double-quote.

                    // The coming symbols will either be a quote or value (in the case that the value is unquoted).

                    SourceLocation symbolStartLocation;

                    // Skip the whitespace preceding the start of the attribute value.
                    do
                    {
                        i++; // Start from the symbol after '='.
                    } while (i < htmlSymbols.Length &&
                        (htmlSymbols[i].Type == HtmlSymbolType.WhiteSpace ||
                        htmlSymbols[i].Type == HtmlSymbolType.NewLine));

                    // Check for attribute start values, aka single or double quote
                    if (i < htmlSymbols.Length && IsQuote(htmlSymbols[i]))
                    {
                        if (htmlSymbols[i].Type == HtmlSymbolType.SingleQuote)
                        {
                            attributeValueStyle = HtmlAttributeValueStyle.SingleQuotes;
                        }

                        symbolStartLocation = htmlSymbols[i].Start;

                        // If there's a start quote then there must be an end quote to be valid, skip it.
                        symbolOffset = 1;
                    }
                    else
                    {
                        // We are at the symbol after equals. Go back to equals to ensure we don't skip past that symbol.
                        i--;

                        symbolStartLocation = symbol.Start;
                    }

                    attributeValueStartLocation = new SourceLocation(
                        symbolStartLocation.FilePath,
                        symbolStartLocation.AbsoluteIndex + 1,
                        symbolStartLocation.LineIndex,
                        symbolStartLocation.CharacterIndex + 1);

                    afterEquals = true;
                }
                else if (symbol.Type == HtmlSymbolType.WhiteSpace)
                {
                    // We're at the start of the attribute, this branch may be hit on the first iterations of
                    // the loop since the parser separates attributes with their spaces included as symbols.
                    // We're at: "| asp-for='...'" or "| asp-for=..."
                    // Note: This will not be hit even for situations like asp-for  ="..." because the core Razor
                    // parser currently does not know how to handle attributes in that format. This will be addressed
                    // by https://github.com/aspnet/Razor/issues/123.

                    attributeValueStartLocation = SourceLocationTracker.Advance(attributeValueStartLocation, symbol.Content);
                }
            }

            // After all symbols have been added we need to set the builders start position so we do not indirectly
            // modify the span's start location.
            builder.Start = attributeValueStartLocation;

            if (name == null)
            {
                // We couldn't find a name, if the original span content was whitespace it ultimately means the tag
                // that owns this "attribute" is malformed and is expecting a user to type a new attribute.
                // ex: <myTH class="btn"| |
                if (!string.IsNullOrWhiteSpace(span.Content))
                {
                    errorSink.OnError(
                        span.Start,
                        LegacyResources.TagHelperBlockRewriter_TagHelperAttributeListMustBeWellFormed,
                        span.Content.Length);
                }

                return null;
            }

            var result = CreateTryParseResult(name, descriptors);

            // If we're not after an equal then we should treat the value as if it were a minimized attribute.
            Span attributeValue = null;
            if (afterEquals)
            {
                attributeValue = CreateMarkupAttribute(builder, result.IsBoundNonStringAttribute);
            }
            else
            {
                attributeValueStyle = HtmlAttributeValueStyle.Minimized;
            }

            result.AttributeValueNode = attributeValue;
            result.AttributeValueStyle = attributeValueStyle;
            return result;
        }

        private static TryParseResult TryParseBlock(
            string tagName,
            Block block,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink)
        {
            // TODO: Accept more than just spans: https://github.com/aspnet/Razor/issues/96.
            // The first child will only ever NOT be a Span if a user is doing something like:
            // <input @checked />

            var childSpan = block.Children.First() as Span;

            if (childSpan == null || childSpan.Kind != SpanKind.Markup)
            {
                errorSink.OnError(
                    block.Start,
                    LegacyResources.FormatTagHelpers_CannotHaveCSharpInTagDeclaration(tagName),
                    block.Length);

                return null;
            }

            var builder = new BlockBuilder(block);

            // If there's only 1 child it means that it's plain text inside of the attribute.
            // i.e. <div class="plain text in attribute">
            if (builder.Children.Count == 1)
            {
                return TryParseSpan(childSpan, descriptors, errorSink);
            }

            var nameSymbols = childSpan
                .Symbols
                .OfType<HtmlSymbol>()
                .SkipWhile(symbol => !HtmlMarkupParser.IsValidAttributeNameSymbol(symbol)) // Skip prefix
                .TakeWhile(nameSymbol => HtmlMarkupParser.IsValidAttributeNameSymbol(nameSymbol))
                .Select(nameSymbol => nameSymbol.Content);

            var name = string.Concat(nameSymbols);
            if (string.IsNullOrEmpty(name))
            {
                errorSink.OnError(
                    childSpan.Start,
                    LegacyResources.FormatTagHelpers_AttributesMustHaveAName(tagName),
                    childSpan.Length);

                return null;
            }

            // Have a name now. Able to determine correct isBoundNonStringAttribute value.
            var result = CreateTryParseResult(name, descriptors);

            var firstChild = builder.Children[0] as Span;
            if (firstChild != null && firstChild.Symbols[0] is HtmlSymbol)
            {
                var htmlSymbol = firstChild.Symbols[firstChild.Symbols.Count - 1] as HtmlSymbol;
                switch (htmlSymbol.Type)
                {
                    // Treat NoQuotes and DoubleQuotes equivalently. We purposefully do not persist NoQuotes
                    // ValueStyles at code generation time to protect users from rendering dynamic content with spaces
                    // that can break attributes.
                    // Ex: <tag my-attribute=@value /> where @value results in the test "hello world".
                    // This way, the above code would render <tag my-attribute="hello world" />.
                    case HtmlSymbolType.Equals:
                    case HtmlSymbolType.DoubleQuote:
                        result.AttributeValueStyle = HtmlAttributeValueStyle.DoubleQuotes;
                        break;
                    case HtmlSymbolType.SingleQuote:
                        result.AttributeValueStyle = HtmlAttributeValueStyle.SingleQuotes;
                        break;
                    default:
                        result.AttributeValueStyle = HtmlAttributeValueStyle.Minimized;
                        break;
                }
            }

            // Remove first child i.e. foo="
            builder.Children.RemoveAt(0);

            // Grabbing last child to check if the attribute value is quoted.
            var endNode = block.Children.Last();
            if (!endNode.IsBlock)
            {
                var endSpan = (Span)endNode;

                // In some malformed cases e.g. <p bar="false', the last Span (false' in the ex.) may contain more
                // than a single HTML symbol. Do not ignore those other symbols.
                var symbolCount = endSpan.Symbols.Count();
                var endSymbol = symbolCount == 1 ? (HtmlSymbol)endSpan.Symbols.First() : null;

                // Checking to see if it's a quoted attribute, if so we should remove end quote
                if (endSymbol != null && IsQuote(endSymbol))
                {
                    builder.Children.RemoveAt(builder.Children.Count - 1);
                }
            }

            // We need to rebuild the chunk generators of the builder and its children (this is needed to
            // ensure we don't do special attribute chunk generation since this is a tag helper).
            block = RebuildChunkGenerators(builder.Build(), result.IsBoundAttribute);

            // If there's only 1 child at this point its value could be a simple markup span (treated differently than
            // block level elements for attributes).
            if (block.Children.Count() == 1)
            {
                var child = block.Children.First() as Span;
                if (child != null)
                {
                    // After pulling apart the block we just have a value span.
                    var spanBuilder = new SpanBuilder(child);

                    result.AttributeValueNode =
                        CreateMarkupAttribute(spanBuilder, result.IsBoundNonStringAttribute);

                    return result;
                }
            }

            var isFirstSpan = true;

            result.AttributeValueNode = ConvertToMarkupAttributeBlock(
                block,
                (parentBlock, span) =>
                {
                    // If the attribute was requested by a tag helper but the corresponding property was not a
                    // string, then treat its value as code. A non-string value can be any C# value so we need
                    // to ensure the SyntaxTreeNode reflects that.
                    if (result.IsBoundNonStringAttribute)
                    {
                        // For bound non-string attributes, we'll only allow a transition span to appear at the very
                        // beginning of the attribute expression. All later transitions would appear as code so that
                        // they are part of the generated output. E.g.
                        // key="@value" -> MyTagHelper.key = value
                        // key=" @value" -> MyTagHelper.key =  @value
                        // key="1 + @case" -> MyTagHelper.key = 1 + @case
                        // key="@int + @case" -> MyTagHelper.key = int + @case
                        // key="@(a + b) -> MyTagHelper.key = a + b
                        // key="4 + @(a + b)" -> MyTagHelper.key = 4 + @(a + b)
                        if (isFirstSpan && span.Kind == SpanKind.Transition)
                        {
                            // do nothing.
                        }
                        else
                        {
                            var spanBuilder = new SpanBuilder(span);

                            if (parentBlock.Type == BlockType.Expression &&
                                (spanBuilder.Kind == SpanKind.Transition ||
                                spanBuilder.Kind == SpanKind.MetaCode))
                            {
                                // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                                spanBuilder.ChunkGenerator = new MarkupChunkGenerator();
                            }

                            ConfigureNonStringAttribute(spanBuilder);

                            span = spanBuilder.Build();
                        }
                    }

                    isFirstSpan = false;

                    return span;
                });

            return result;
        }

        private static Block ConvertToMarkupAttributeBlock(
            Block block,
            Func<Block, Span, Span> createMarkupAttribute)
        {
            var blockBuilder = new BlockBuilder
            {
                ChunkGenerator = block.ChunkGenerator,
                Type = block.Type
            };

            foreach (var child in block.Children)
            {
                SyntaxTreeNode markupAttributeChild;

                if (child.IsBlock)
                {
                    markupAttributeChild = ConvertToMarkupAttributeBlock((Block)child, createMarkupAttribute);
                }
                else
                {
                    markupAttributeChild = createMarkupAttribute(block, (Span)child);
                }

                blockBuilder.Children.Add(markupAttributeChild);
            }

            return blockBuilder.Build();
        }

        private static Block RebuildChunkGenerators(Block block, bool isBound)
        {
            var builder = new BlockBuilder(block);

            // Don't want to rebuild unbound dynamic attributes. They need to run through the conditional attribute
            // removal system at runtime. A conditional attribute at the parse tree rewriting level is defined by
            // having at least 1 child with a DynamicAttributeBlockChunkGenerator.
            if (!isBound &&
                block.Children.Any(
                    child => child.IsBlock &&
                    ((Block)child).ChunkGenerator is DynamicAttributeBlockChunkGenerator))
            {
                // The parent chunk generator must be removed because it's normally responsible for conditionally
                // generating the attribute prefix (class=") and suffix ("). The prefix and suffix concepts aren't
                // applicable for the TagHelper use case since the attributes are put into a dictionary like object as
                // name value pairs.
                builder.ChunkGenerator = ParentChunkGenerator.Null;

                return builder.Build();
            }

            var isDynamic = builder.ChunkGenerator is DynamicAttributeBlockChunkGenerator;

            // We don't want any attribute specific logic here, null out the block chunk generator.
            if (isDynamic || builder.ChunkGenerator is AttributeBlockChunkGenerator)
            {
                builder.ChunkGenerator = ParentChunkGenerator.Null;
            }

            for (var i = 0; i < builder.Children.Count; i++)
            {
                var child = builder.Children[i];

                if (child.IsBlock)
                {
                    // The child is a block, recurse down into the block to rebuild its children
                    builder.Children[i] = RebuildChunkGenerators((Block)child, isBound);
                }
                else
                {
                    var childSpan = (Span)child;
                    ISpanChunkGenerator newChunkGenerator = null;
                    var literalGenerator = childSpan.ChunkGenerator as LiteralAttributeChunkGenerator;

                    if (literalGenerator != null)
                    {
                        newChunkGenerator = new MarkupChunkGenerator();
                    }
                    else if (isDynamic && childSpan.ChunkGenerator == SpanChunkGenerator.Null)
                    {
                        // Usually the dynamic chunk generator handles creating the null chunk generators underneath
                        // it. This doesn't make sense in terms of tag helpers though, we need to change null code
                        // generators to markup chunk generators.

                        newChunkGenerator = new MarkupChunkGenerator();
                    }

                    // If we have a new chunk generator we'll need to re-build the child
                    if (newChunkGenerator != null)
                    {
                        var childSpanBuilder = new SpanBuilder(childSpan)
                        {
                            ChunkGenerator = newChunkGenerator
                        };

                        builder.Children[i] = childSpanBuilder.Build();
                    }
                }
            }

            return builder.Build();
        }

        private static SourceLocation GetAttributeNameStartLocation(SyntaxTreeNode node)
        {
            Span span;
            var nodeStart = SourceLocation.Undefined;

            if (node.IsBlock)
            {
                span = ((Block)node).FindFirstDescendentSpan();
                nodeStart = span.Parent.Start;
            }
            else
            {
                span = (Span)node;
                nodeStart = span.Start;
            }

            // Span should never be null here, this should only ever be called if an attribute was successfully parsed.
            Debug.Assert(span != null);

            // Attributes must have at least one non-whitespace character to represent the tagName (even if its a C#
            // expression).
            var firstNonWhitespaceSymbol = span
                .Symbols
                .OfType<HtmlSymbol>()
                .First(sym => sym.Type != HtmlSymbolType.WhiteSpace && sym.Type != HtmlSymbolType.NewLine);

            return firstNonWhitespaceSymbol.Start;
        }

        private static Span CreateMarkupAttribute(SpanBuilder builder, bool isBoundNonStringAttribute)
        {
            Debug.Assert(builder != null);

            // If the attribute was requested by a tag helper but the corresponding property was not a string,
            // then treat its value as code. A non-string value can be any C# value so we need to ensure the
            // SyntaxTreeNode reflects that.
            if (isBoundNonStringAttribute)
            {
                ConfigureNonStringAttribute(builder);
            }

            return builder.Build();
        }

        private static bool IsNullOrWhitespaceAttributeValue(SyntaxTreeNode attributeValue)
        {
            if (attributeValue.IsBlock)
            {
                foreach (var span in ((Block)attributeValue).Flatten())
                {
                    if (!string.IsNullOrWhiteSpace(span.Content))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return string.IsNullOrWhiteSpace(((Span)attributeValue).Content);
            }
        }

        // Determines the full name of the Type of the property corresponding to an attribute with the given name.
        private static string GetPropertyType(string name, IEnumerable<TagHelperDescriptor> descriptors)
        {
            var firstBoundAttribute = FindFirstBoundAttribute(name, descriptors);

            return firstBoundAttribute?.TypeName;
        }

        // Create a TryParseResult for given name, filling in binding details.
        private static TryParseResult CreateTryParseResult(string name, IEnumerable<TagHelperDescriptor> descriptors)
        {
            var firstBoundAttribute = FindFirstBoundAttribute(name, descriptors);
            var isBoundAttribute = firstBoundAttribute != null;
            var isBoundNonStringAttribute = isBoundAttribute && !firstBoundAttribute.IsStringProperty;
            var isMissingDictionaryKey = isBoundAttribute &&
                firstBoundAttribute.IsIndexer &&
                name.Length == firstBoundAttribute.Name.Length;

            return new TryParseResult
            {
                AttributeName = name,
                IsBoundAttribute = isBoundAttribute,
                IsBoundNonStringAttribute = isBoundNonStringAttribute,
                IsMissingDictionaryKey = isMissingDictionaryKey,
            };
        }

        // Finds first TagHelperAttributeDescriptor matching given name.
        private static TagHelperAttributeDescriptor FindFirstBoundAttribute(
            string name,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            // Non-indexers (exact HTML attribute name matches) have higher precedence than indexers (prefix matches).
            // Attributes already sorted to ensure this precedence.
            var firstBoundAttribute = descriptors
                .SelectMany(descriptor => descriptor.Attributes)
                .FirstOrDefault(attributeDescriptor => attributeDescriptor.IsNameMatch(name));

            return firstBoundAttribute;
        }

        private static bool IsQuote(HtmlSymbol htmlSymbol)
        {
            return htmlSymbol.Type == HtmlSymbolType.DoubleQuote ||
                   htmlSymbol.Type == HtmlSymbolType.SingleQuote;
        }

        private static void ConfigureNonStringAttribute(SpanBuilder builder)
        {
            builder.Kind = SpanKind.Code;
            builder.EditHandler = new ImplicitExpressionEditHandler(
                    builder.EditHandler.Tokenizer,
                    CSharpCodeParser.DefaultKeywords,
                    acceptTrailingDot: true)
            {
                AcceptedCharacters = AcceptedCharacters.AnyExceptNewline
            };
        }

        private class TryParseResult
        {
            public string AttributeName { get; set; }

            public SyntaxTreeNode AttributeValueNode { get; set; }

            public HtmlAttributeValueStyle AttributeValueStyle { get; set; }

            public bool IsBoundAttribute { get; set; }

            public bool IsBoundNonStringAttribute { get; set; }

            public bool IsMissingDictionaryKey { get; set; }
        }
    }
}