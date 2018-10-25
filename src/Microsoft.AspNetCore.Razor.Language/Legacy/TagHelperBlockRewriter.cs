// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class TagHelperBlockRewriter
    {
        private static readonly string StringTypeName = typeof(string).FullName;

        public static TagHelperBlockBuilder Rewrite(
            string tagName,
            bool validStructure,
            RazorParserFeatureFlags featureFlags,
            Block tag,
            TagHelperBinding bindingResult,
            ErrorSink errorSink)
        {
            // There will always be at least one child for the '<'.
            var start = tag.Children.First().Start;
            var attributes = GetTagAttributes(tagName, validStructure, tag, bindingResult, errorSink, featureFlags);
            var tagMode = GetTagMode(tagName, tag, bindingResult, errorSink);

            return new TagHelperBlockBuilder(tagName, tagMode, start, attributes, bindingResult);
        }

        private static IList<TagHelperAttributeNode> GetTagAttributes(
            string tagName,
            bool validStructure,
            Block tagBlock,
            TagHelperBinding bindingResult,
            ErrorSink errorSink,
            RazorParserFeatureFlags featureFlags)
        {
            var attributes = new List<TagHelperAttributeNode>();

            // We skip the first child "<tagname" and take everything up to the ending portion of the tag ">" or "/>".
            // The -2 accounts for both the start and end tags. If the tag does not have a valid structure then there's
            // no end tag to ignore.
            var tokenOffset = validStructure ? 2 : 1;
            var attributeChildren = tagBlock.Children.Skip(1).Take(tagBlock.Children.Count() - tokenOffset);
            var processedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in attributeChildren)
            {
                TryParseResult result;
                if (child.IsBlock)
                {
                    result = TryParseBlock(tagName, (Block)child, bindingResult.Descriptors, errorSink, processedBoundAttributeNames);
                }
                else
                {
                    result = TryParseSpan((Span)child, bindingResult.Descriptors, errorSink, processedBoundAttributeNames);
                }

                // Only want to track the attribute if we succeeded in parsing its corresponding Block/Span.
                if (result != null)
                {
                    // Check if it's a non-boolean bound attribute that is minimized or if it's a bound
                    // non-string attribute that has null or whitespace content.
                    var isMinimized = result.AttributeValueNode == null;
                    var isValidMinimizedAttribute = featureFlags.AllowMinimizedBooleanTagHelperAttributes && result.IsBoundBooleanAttribute;
                    if ((isMinimized &&
                        result.IsBoundAttribute &&
                        !isValidMinimizedAttribute) ||
                        (!isMinimized &&
                        result.IsBoundNonStringAttribute &&
                         IsNullOrWhitespaceAttributeValue(result.AttributeValueNode)))
                    {
                        var errorLocation = GetAttributeNameLocation(child, result.AttributeName);
                        var propertyTypeName = GetPropertyType(result.AttributeName, bindingResult.Descriptors);
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(errorLocation, result.AttributeName, tagName, propertyTypeName);
                        errorSink.OnError(diagnostic);
                    }

                    // Check if the attribute was a prefix match for a tag helper dictionary property but the
                    // dictionary key would be the empty string.
                    if (result.IsMissingDictionaryKey)
                    {
                        var errorLocation = GetAttributeNameLocation(child, result.AttributeName);
                        var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(errorLocation, result.AttributeName, tagName);
                        errorSink.OnError(diagnostic);
                    }

                    var attributeNode = new TagHelperAttributeNode(
                        result.AttributeName,
                        result.AttributeValueNode,
                        result.AttributeStructure);

                    attributes.Add(attributeNode);
                }
                else
                {
                    // Error occurred while parsing the attribute. Don't try parsing the rest to avoid misleading errors.
                    break;
                }
            }

            return attributes;
        }

        private static TagMode GetTagMode(
            string tagName,
            Block beginTagBlock,
            TagHelperBinding bindingResult,
            ErrorSink errorSink)
        {
            var childSpan = beginTagBlock.FindLastDescendentSpan();

            // Self-closing tags are always valid despite descriptors[X].TagStructure.
            if (childSpan?.Content.EndsWith("/>", StringComparison.Ordinal) ?? false)
            {
                return TagMode.SelfClosing;
            }

            foreach (var descriptor in bindingResult.Descriptors)
            {
                var boundRules = bindingResult.GetBoundRules(descriptor);
                var nonDefaultRule = boundRules.FirstOrDefault(rule => rule.TagStructure != TagStructure.Unspecified);

                if (nonDefaultRule?.TagStructure == TagStructure.WithoutEndTag)
                {
                    return TagMode.StartTagOnly;
                }
            }

            return TagMode.StartTagAndEndTag;
        }

        // This method handles cases when the attribute is a simple span attribute such as
        // class="something moresomething".  This does not handle complex attributes such as
        // class="@myclass". Therefore the span.Content is equivalent to the entire attribute.
        private static TryParseResult TryParseSpan(
            Span span,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink,
            HashSet<string> processedBoundAttributeNames)
        {
            var afterEquals = false;
            var builder = new SpanBuilder(span.Start)
            {
                ChunkGenerator = span.ChunkGenerator,
                EditHandler = span.EditHandler,
                Kind = span.Kind
            };

            // Will contain tokens that represent a single attribute value: <input| class="btn"| />
            var tokens = span.Tokens;
            var capturedAttributeValueStart = false;
            var attributeValueStartLocation = span.Start;

            // Default to DoubleQuotes. We purposefully do not persist NoQuotes ValueStyle to stay consistent with the
            // TryParseBlock() variation of attribute parsing.
            var attributeValueStyle = AttributeStructure.DoubleQuotes;

            // The tokenOffset is initialized to 0 to expect worst case: "class=". If a quote is found later on for
            // the attribute value the tokenOffset is adjusted accordingly.
            var tokenOffset = 0;
            string name = null;

            // Iterate down through the tokens to find the name and the start of the value.
            // We subtract the tokenOffset so we don't accept an ending quote of a span.
            for (var i = 0; i < tokens.Count - tokenOffset; i++)
            {
                var token = tokens[i];

                if (afterEquals)
                {
                    // We've captured all leading whitespace, the attribute name, and an equals with an optional
                    // quote/double quote. We're now at: " asp-for='|...'" or " asp-for=|..."
                    // The goal here is to capture all tokens until the end of the attribute. Note this will not
                    // consume an ending quote due to the tokenOffset.

                    // When tokens are accepted into SpanBuilders, their locations get altered to be offset by the
                    // parent which is why we need to mark our start location prior to adding the token.
                    // This is needed to know the location of the attribute value start within the document.
                    if (!capturedAttributeValueStart)
                    {
                        capturedAttributeValueStart = true;

                        attributeValueStartLocation = token.Start;
                    }

                    builder.Accept(token.Green);
                }
                else if (name == null && HtmlMarkupParser.IsValidAttributeNameToken(token.Green))
                {
                    // We've captured all leading whitespace prior to the attribute name.
                    // We're now at: " |asp-for='...'" or " |asp-for=..."
                    // The goal here is to capture the attribute name.

                    var nameBuilder = new StringBuilder();
                    // Move the indexer past the attribute name tokens.
                    for (var j = i; j < tokens.Count; j++)
                    {
                        var nameToken = tokens[j];
                        if (!HtmlMarkupParser.IsValidAttributeNameToken(nameToken.Green))
                        {
                            break;
                        }

                        nameBuilder.Append(nameToken.Content);
                        i++;
                    }

                    i--;

                    name = nameBuilder.ToString();
                    attributeValueStartLocation = SourceLocationTracker.Advance(attributeValueStartLocation, name);
                }
                else if (token.Kind == SyntaxKind.Equals)
                {
                    // We've captured all leading whitespace and the attribute name.
                    // We're now at: " asp-for|='...'" or " asp-for|=..."
                    // The goal here is to consume the equal sign and the optional single/double-quote.

                    // The coming tokens will either be a quote or value (in the case that the value is unquoted).

                    SourceLocation tokenStartLocation;

                    // Skip the whitespace preceding the start of the attribute value.
                    do
                    {
                        i++; // Start from the token after '='.
                    } while (i < tokens.Count &&
                        (tokens[i].Kind == SyntaxKind.Whitespace ||
                        tokens[i].Kind == SyntaxKind.NewLine));

                    // Check for attribute start values, aka single or double quote
                    if (i < tokens.Count && IsQuote(tokens[i]))
                    {
                        if (tokens[i].Kind == SyntaxKind.SingleQuote)
                        {
                            attributeValueStyle = AttributeStructure.SingleQuotes;
                        }

                        tokenStartLocation = tokens[i].Start;

                        // If there's a start quote then there must be an end quote to be valid, skip it.
                        tokenOffset = 1;
                    }
                    else
                    {
                        // We are at the token after equals. Go back to equals to ensure we don't skip past that token.
                        i--;

                        tokenStartLocation = token.Start;
                    }

                    attributeValueStartLocation = new SourceLocation(
                        tokenStartLocation.FilePath,
                        tokenStartLocation.AbsoluteIndex + 1,
                        tokenStartLocation.LineIndex,
                        tokenStartLocation.CharacterIndex + 1);

                    afterEquals = true;
                }
                else if (token.Kind == SyntaxKind.Whitespace)
                {
                    // We're at the start of the attribute, this branch may be hit on the first iterations of
                    // the loop since the parser separates attributes with their spaces included as tokens.
                    // We're at: "| asp-for='...'" or "| asp-for=..."
                    // Note: This will not be hit even for situations like asp-for  ="..." because the core Razor
                    // parser currently does not know how to handle attributes in that format. This will be addressed
                    // by https://github.com/aspnet/Razor/issues/123.

                    attributeValueStartLocation = SourceLocationTracker.Advance(attributeValueStartLocation, token.Content);
                }
            }

            // After all tokens have been added we need to set the builders start position so we do not indirectly
            // modify the span's start location.
            builder.Start = attributeValueStartLocation;

            if (name == null)
            {
                // We couldn't find a name, if the original span content was whitespace it ultimately means the tag
                // that owns this "attribute" is malformed and is expecting a user to type a new attribute.
                // ex: <myTH class="btn"| |
                if (!string.IsNullOrWhiteSpace(span.Content))
                {
                    var location = new SourceSpan(span.Start, span.Content.Length);
                    var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelperAttributeListMustBeWellFormed(location);
                    errorSink.OnError(diagnostic);
                }

                return null;
            }

            var result = CreateTryParseResult(name, descriptors, processedBoundAttributeNames);

            // If we're not after an equal then we should treat the value as if it were a minimized attribute.
            Span attributeValue = null;
            if (afterEquals)
            {
                attributeValue = CreateMarkupAttribute(builder, result);
            }
            else
            {
                attributeValueStyle = AttributeStructure.Minimized;
            }

            result.AttributeValueNode = attributeValue;
            result.AttributeStructure = attributeValueStyle;
            return result;
        }

        private static TryParseResult TryParseBlock(
            string tagName,
            Block block,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink,
            HashSet<string> processedBoundAttributeNames)
        {
            // TODO: Accept more than just spans: https://github.com/aspnet/Razor/issues/96.
            // The first child will only ever NOT be a Span if a user is doing something like:
            // <input @checked />

            var childSpan = block.Children.First() as Span;

            if (childSpan == null || childSpan.Kind != SpanKindInternal.Markup)
            {
                var location = new SourceSpan(block.Start, block.Length);
                var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(location, tagName);
                errorSink.OnError(diagnostic);

                return null;
            }

            var builder = new BlockBuilder(block);

            // If there's only 1 child it means that it's plain text inside of the attribute.
            // i.e. <div class="plain text in attribute">
            if (builder.Children.Count == 1)
            {
                return TryParseSpan(childSpan, descriptors, errorSink, processedBoundAttributeNames);
            }

            var nameTokens = childSpan
                .Tokens
                .SkipWhile(token => !HtmlMarkupParser.IsValidAttributeNameToken(token.Green)) // Skip prefix
                .TakeWhile(nameToken => HtmlMarkupParser.IsValidAttributeNameToken(nameToken.Green))
                .Select(nameToken => nameToken.Content);

            var name = string.Concat(nameTokens);
            if (string.IsNullOrEmpty(name))
            {
                var location = new SourceSpan(childSpan.Start, childSpan.Length);
                var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelperAttributesMustHaveAName(location, tagName);
                errorSink.OnError(diagnostic);

                return null;
            }

            // Have a name now. Able to determine correct isBoundNonStringAttribute value.
            var result = CreateTryParseResult(name, descriptors, processedBoundAttributeNames);

            var firstChild = builder.Children[0] as Span;
            if (firstChild != null)
            {
                var token = firstChild.Tokens[firstChild.Tokens.Count - 1];
                switch (token.Kind)
                {
                    case SyntaxKind.Equals:
                        if (builder.Children.Count == 2 &&
                            builder.Children[1] is Span value &&
                            value.Kind == SpanKindInternal.Markup)
                        {
                            // Attribute value is a string literal. Eg: <tag my-attribute=foo />.
                            result.AttributeStructure = AttributeStructure.NoQuotes;
                        }
                        else
                        {
                            // Could be an expression, treat NoQuotes and DoubleQuotes equivalently. We purposefully do not persist NoQuotes
                            // ValueStyles at code generation time to protect users from rendering dynamic content with spaces
                            // that can break attributes.
                            // Ex: <tag my-attribute=@value /> where @value results in the test "hello world".
                            // This way, the above code would render <tag my-attribute="hello world" />.
                            result.AttributeStructure = AttributeStructure.DoubleQuotes;
                        }
                        break;
                    case SyntaxKind.DoubleQuote:
                        result.AttributeStructure = AttributeStructure.DoubleQuotes;
                        break;
                    case SyntaxKind.SingleQuote:
                        result.AttributeStructure = AttributeStructure.SingleQuotes;
                        break;
                    default:
                        result.AttributeStructure = AttributeStructure.Minimized;
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
                // than a single HTML token. Do not ignore those other tokens.
                var tokenCount = endSpan.Tokens.Count;
                var endToken = tokenCount == 1 ? endSpan.Tokens.First() : null;

                // Checking to see if it's a quoted attribute, if so we should remove end quote
                if (endToken != null && IsQuote(endToken))
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
                        CreateMarkupAttribute(spanBuilder, result);

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
                        if (isFirstSpan && span.Kind == SpanKindInternal.Transition)
                        {
                            // do nothing.
                        }
                        else
                        {
                            var spanBuilder = new SpanBuilder(span);

                            if (parentBlock.Type == BlockKindInternal.Expression &&
                                (spanBuilder.Kind == SpanKindInternal.Transition ||
                                spanBuilder.Kind == SpanKindInternal.MetaCode))
                            {
                                // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                                spanBuilder.ChunkGenerator = new MarkupChunkGenerator();
                            }

                            ConfigureNonStringAttribute(spanBuilder, result.IsDuplicateAttribute);

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

        private static SourceSpan GetAttributeNameLocation(SyntaxTreeNode node, string attributeName)
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
            var firstNonWhitespaceToken = span
                .Tokens
                .First(token => token.Kind != SyntaxKind.Whitespace && token.Kind != SyntaxKind.NewLine);

            var location = new SourceSpan(firstNonWhitespaceToken.Start, attributeName.Length);
            return location;
        }

        private static Span CreateMarkupAttribute(SpanBuilder builder, TryParseResult result)
        {
            Debug.Assert(builder != null);

            // If the attribute was requested by a tag helper but the corresponding property was not a string,
            // then treat its value as code. A non-string value can be any C# value so we need to ensure the
            // SyntaxTreeNode reflects that.
            if (result.IsBoundNonStringAttribute)
            {
                ConfigureNonStringAttribute(builder, result.IsDuplicateAttribute);
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
            var isBoundToIndexer = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(name, firstBoundAttribute);

            if (isBoundToIndexer)
            {
                return firstBoundAttribute?.IndexerTypeName;
            }
            else
            {
                return firstBoundAttribute?.TypeName;
            }
        }

        // Create a TryParseResult for given name, filling in binding details.
        private static TryParseResult CreateTryParseResult(
            string name,
            IEnumerable<TagHelperDescriptor> descriptors,
            HashSet<string> processedBoundAttributeNames)
        {
            var firstBoundAttribute = FindFirstBoundAttribute(name, descriptors);
            var isBoundAttribute = firstBoundAttribute != null;
            var isBoundNonStringAttribute = isBoundAttribute && !firstBoundAttribute.ExpectsStringValue(name);
            var isBoundBooleanAttribute = isBoundAttribute && firstBoundAttribute.ExpectsBooleanValue(name);
            var isMissingDictionaryKey = isBoundAttribute &&
                firstBoundAttribute.IndexerNamePrefix != null &&
                name.Length == firstBoundAttribute.IndexerNamePrefix.Length;

            var isDuplicateAttribute = false;
            if (isBoundAttribute && !processedBoundAttributeNames.Add(name))
            {
                // A bound attribute with the same name has already been processed.
                isDuplicateAttribute = true;
            }

            return new TryParseResult
            {
                AttributeName = name,
                IsBoundAttribute = isBoundAttribute,
                IsBoundNonStringAttribute = isBoundNonStringAttribute,
                IsBoundBooleanAttribute = isBoundBooleanAttribute,
                IsMissingDictionaryKey = isMissingDictionaryKey,
                IsDuplicateAttribute = isDuplicateAttribute
            };
        }

        // Finds first TagHelperAttributeDescriptor matching given name.
        private static BoundAttributeDescriptor FindFirstBoundAttribute(
            string name,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            var firstBoundAttribute = descriptors
                .SelectMany(descriptor => descriptor.BoundAttributes)
                .FirstOrDefault(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(name, attributeDescriptor));

            return firstBoundAttribute;
        }

        private static bool IsQuote(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.DoubleQuote ||
                   token.Kind == SyntaxKind.SingleQuote;
        }

        private static void ConfigureNonStringAttribute(SpanBuilder builder, bool isDuplicateAttribute)
        {
            builder.Kind = SpanKindInternal.Code;
            builder.EditHandler = new ImplicitExpressionEditHandler(
                    builder.EditHandler.Tokenizer,
                    CSharpCodeParser.DefaultKeywords,
                    acceptTrailingDot: true)
            {
                AcceptedCharacters = AcceptedCharactersInternal.AnyExceptNewline
            };

            if (!isDuplicateAttribute && builder.ChunkGenerator != SpanChunkGenerator.Null)
            {
                // We want to mark the value of non-string bound attributes to be CSharp.
                // Except in two cases,
                // 1. Cases when we don't want to render the span. Eg: Transition span '@'.
                // 2. Cases when it is a duplicate of a bound attribute. This should just be rendered as html.

                builder.ChunkGenerator = new ExpressionChunkGenerator();
            }
        }

        private class TryParseResult
        {
            public string AttributeName { get; set; }

            public SyntaxTreeNode AttributeValueNode { get; set; }

            public AttributeStructure AttributeStructure { get; set; }

            public bool IsBoundAttribute { get; set; }

            public bool IsBoundNonStringAttribute { get; set; }

            public bool IsBoundBooleanAttribute { get; set; }

            public bool IsMissingDictionaryKey { get; set; }

            public bool IsDuplicateAttribute { get; set; }
        }
    }
}
