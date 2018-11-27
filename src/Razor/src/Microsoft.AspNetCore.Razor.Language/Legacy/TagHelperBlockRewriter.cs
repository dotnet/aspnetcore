// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class TagHelperBlockRewriter
    {
        private static readonly string StringTypeName = typeof(string).FullName;

        public static MarkupTagHelperStartTagSyntax Rewrite(
            string tagName,
            bool validStructure,
            RazorParserFeatureFlags featureFlags,
            MarkupTagBlockSyntax tag,
            TagHelperBinding bindingResult,
            ErrorSink errorSink,
            RazorSourceDocument source)
        {
            // There will always be at least one child for the '<'.
            var rewrittenChildren = GetRewrittenChildren(tagName, validStructure, tag, bindingResult, featureFlags, errorSink, source);

            return SyntaxFactory.MarkupTagHelperStartTag(rewrittenChildren);
        }

        public static TagMode GetTagMode(
            MarkupTagBlockSyntax tagBlock,
            TagHelperBinding bindingResult,
            ErrorSink errorSink)
        {
            var childSpan = tagBlock.GetLastToken()?.Parent;

            // Self-closing tags are always valid despite descriptors[X].TagStructure.
            if (childSpan?.GetContent().EndsWith("/>", StringComparison.Ordinal) ?? false)
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

        private static SyntaxList<RazorSyntaxNode> GetRewrittenChildren(
            string tagName,
            bool validStructure,
            MarkupTagBlockSyntax tagBlock,
            TagHelperBinding bindingResult,
            RazorParserFeatureFlags featureFlags,
            ErrorSink errorSink,
            RazorSourceDocument source)
        {
            var tagHelperBuilder = SyntaxListBuilder<RazorSyntaxNode>.Create();
            var processedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (tagBlock.Children.Count == 1)
            {
                // Tag with no attributes. We have nothing to rewrite here.
                return tagBlock.Children;
            }

            // Add the tag start
            tagHelperBuilder.Add(tagBlock.Children.First());

            // We skip the first child "<tagname" and take everything up to the ending portion of the tag ">" or "/>".
            // If the tag does not have a valid structure then there's no close angle to ignore.
            var tokenOffset = validStructure ? 1 : 0;
            for (var i = 1; i < tagBlock.Children.Count - tokenOffset; i++)
            {
                var isMinimized = false;
                var attributeNameLocation = SourceLocation.Undefined;
                var child = tagBlock.Children[i];
                TryParseResult result;
                if (child is MarkupAttributeBlockSyntax attributeBlock)
                {
                    attributeNameLocation = attributeBlock.Name.GetSourceLocation(source);
                    result = TryParseAttribute(
                        tagName,
                        attributeBlock,
                        bindingResult.Descriptors,
                        errorSink,
                        processedBoundAttributeNames);
                    tagHelperBuilder.Add(result.RewrittenAttribute);
                }
                else if (child is MarkupMinimizedAttributeBlockSyntax minimizedAttributeBlock)
                {
                    isMinimized = true;
                    attributeNameLocation = minimizedAttributeBlock.Name.GetSourceLocation(source);
                    result = TryParseMinimizedAttribute(
                        tagName,
                        minimizedAttributeBlock,
                        bindingResult.Descriptors,
                        errorSink,
                        processedBoundAttributeNames);
                    tagHelperBuilder.Add(result.RewrittenAttribute);
                }
                else if (child is CSharpCodeBlockSyntax)
                {
                    // TODO: Accept more than just Markup attributes: https://github.com/aspnet/Razor/issues/96.
                    // Something like:
                    // <input @checked />
                    var location = new SourceSpan(child.GetSourceLocation(source), child.FullWidth);
                    var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(location, tagName);
                    errorSink.OnError(diagnostic);

                    result = null;
                }
                else if (child is MarkupTextLiteralSyntax)
                {
                    // If the original span content was whitespace it ultimately means the tag
                    // that owns this "attribute" is malformed and is expecting a user to type a new attribute.
                    // ex: <myTH class="btn"| |
                    var literalContent = child.GetContent();
                    if (!string.IsNullOrWhiteSpace(literalContent))
                    {
                        var location = child.GetSourceSpan(source);
                        var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelperAttributeListMustBeWellFormed(location);
                        errorSink.OnError(diagnostic);
                    }
                    result = null;
                }
                else
                {
                    result = null;
                }

                // Only want to track the attribute if we succeeded in parsing its corresponding Block/Span.
                if (result == null)
                {
                    // Error occurred while parsing the attribute. Don't try parsing the rest to avoid misleading errors.
                    for (var j = i; j < tagBlock.Children.Count; j++)
                    {
                        tagHelperBuilder.Add(tagBlock.Children[j]);
                    }

                    return tagHelperBuilder.ToList();
                }

                // Check if it's a non-boolean bound attribute that is minimized or if it's a bound
                // non-string attribute that has null or whitespace content.
                var isValidMinimizedAttribute = featureFlags.AllowMinimizedBooleanTagHelperAttributes && result.IsBoundBooleanAttribute;
                if ((isMinimized &&
                    result.IsBoundAttribute &&
                    !isValidMinimizedAttribute) ||
                    (!isMinimized &&
                    result.IsBoundNonStringAttribute &&
                     string.IsNullOrWhiteSpace(GetAttributeValueContent(result.RewrittenAttribute))))
                {
                    var errorLocation = new SourceSpan(attributeNameLocation, result.AttributeName.Length);
                    var propertyTypeName = GetPropertyType(result.AttributeName, bindingResult.Descriptors);
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(errorLocation, result.AttributeName, tagName, propertyTypeName);
                    errorSink.OnError(diagnostic);
                }

                // Check if the attribute was a prefix match for a tag helper dictionary property but the
                // dictionary key would be the empty string.
                if (result.IsMissingDictionaryKey)
                {
                    var errorLocation = new SourceSpan(attributeNameLocation, result.AttributeName.Length);
                    var diagnostic = RazorDiagnosticFactory.CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(errorLocation, result.AttributeName, tagName);
                    errorSink.OnError(diagnostic);
                }
            }

            if (validStructure)
            {
                // Add the tag end.
                tagHelperBuilder.Add(tagBlock.Children[tagBlock.Children.Count - 1]);
            }

            return tagHelperBuilder.ToList();
        }

        private static TryParseResult TryParseMinimizedAttribute(
            string tagName,
            MarkupMinimizedAttributeBlockSyntax attributeBlock,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink,
            HashSet<string> processedBoundAttributeNames)
        {
            // Have a name now. Able to determine correct isBoundNonStringAttribute value.
            var result = CreateTryParseResult(attributeBlock.Name.GetContent(), descriptors, processedBoundAttributeNames);

            result.AttributeStructure = AttributeStructure.Minimized;
            var rewritten = SyntaxFactory.MarkupMinimizedTagHelperAttribute(
                attributeBlock.NamePrefix,
                attributeBlock.Name);

            rewritten = rewritten.WithTagHelperAttributeInfo(
                new TagHelperAttributeInfo(result.AttributeName, result.AttributeStructure, result.IsBoundAttribute));

            result.RewrittenAttribute = rewritten;

            return result;
        }

        private static TryParseResult TryParseAttribute(
            string tagName,
            MarkupAttributeBlockSyntax attributeBlock,
            IEnumerable<TagHelperDescriptor> descriptors,
            ErrorSink errorSink,
            HashSet<string> processedBoundAttributeNames)
        {
            // Have a name now. Able to determine correct isBoundNonStringAttribute value.
            var result = CreateTryParseResult(attributeBlock.Name.GetContent(), descriptors, processedBoundAttributeNames);

            if (attributeBlock.ValuePrefix == null)
            {
                // We are purposefully not persisting NoQuotes even for unbound attributes because it is still possible to
                // rewrite the values that introduces a space like in UrlResolutionTagHelper.
                // The other case is it could be an expression, treat NoQuotes and DoubleQuotes equivalently. We purposefully do not persist NoQuotes
                // ValueStyles at code generation time to protect users from rendering dynamic content with spaces
                // that can break attributes.
                // Ex: <tag my-attribute=@value /> where @value results in the test "hello world".
                // This way, the above code would render <tag my-attribute="hello world" />.
                result.AttributeStructure = AttributeStructure.DoubleQuotes;
            }
            else
            {
                var lastToken = attributeBlock.ValuePrefix.GetLastToken();
                switch (lastToken.Kind)
                {
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

            var attributeValue = attributeBlock.Value;
            if (attributeValue == null)
            {
                var builder = SyntaxListBuilder<RazorSyntaxNode>.Create();

                // Add a marker for attribute value when there are no quotes like, <p class= >
                builder.Add(SyntaxFactory.MarkupTextLiteral(new SyntaxList<SyntaxToken>()));

                attributeValue = SyntaxFactory.GenericBlock(builder.ToList());
            }
            var rewrittenValue = RewriteAttributeValue(result, attributeValue);

            var rewritten = SyntaxFactory.MarkupTagHelperAttribute(
                attributeBlock.NamePrefix,
                attributeBlock.Name,
                attributeBlock.NameSuffix,
                attributeBlock.EqualsToken,
                attributeBlock.ValuePrefix,
                rewrittenValue,
                attributeBlock.ValueSuffix);

            rewritten = rewritten.WithTagHelperAttributeInfo(
                new TagHelperAttributeInfo(result.AttributeName, result.AttributeStructure, result.IsBoundAttribute));

            result.RewrittenAttribute = rewritten;

            return result;
        }

        private static MarkupTagHelperAttributeValueSyntax RewriteAttributeValue(TryParseResult result, RazorBlockSyntax attributeValue)
        {
            var rewriter = new AttributeValueRewriter(result);
            var rewrittenValue = attributeValue;
            if (result.IsBoundAttribute)
            {
                // If the attribute was requested by a tag helper but the corresponding property was not a
                // string, then treat its value as code. A non-string value can be any C# value so we need
                // to ensure the tree reflects that.
                rewrittenValue = (RazorBlockSyntax)rewriter.Visit(attributeValue);
            }

            return SyntaxFactory.MarkupTagHelperAttributeValue(rewrittenValue.Children);
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

        private static string GetAttributeValueContent(RazorSyntaxNode attributeBlock)
        {
            if (attributeBlock is MarkupTagHelperAttributeSyntax tagHelperAttribute)
            {
                return tagHelperAttribute.Value?.GetContent();
            }
            else if (attributeBlock is MarkupAttributeBlockSyntax attribute)
            {
                return attribute.Value?.GetContent();
            }

            return null;
        }

        private class AttributeValueRewriter : SyntaxRewriter
        {
            private readonly TryParseResult _tryParseResult;
            private bool _rewriteAsMarkup = false;

            public AttributeValueRewriter(TryParseResult result)
            {
                _tryParseResult = result;
            }

            public override SyntaxNode VisitCSharpTransition(CSharpTransitionSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitCSharpTransition(node);
                }

                // For bound non-string attributes, we'll only allow a transition span to appear at the very
                // beginning of the attribute expression. All later transitions would appear as code so that
                // they are part of the generated output. E.g.
                // key="@value" -> MyTagHelper.key = value
                // key=" @value" -> MyTagHelper.key =  @value
                // key="1 + @case" -> MyTagHelper.key = 1 + @case
                // key="@int + @case" -> MyTagHelper.key = int + @case
                // key="@(a + b) -> MyTagHelper.key = a + b
                // key="4 + @(a + b)" -> MyTagHelper.key = 4 + @(a + b)
                if (_rewriteAsMarkup)
                {
                    // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                    var context = node.GetSpanContext();
                    var newContext = new SpanContext(new MarkupChunkGenerator(), context.EditHandler);

                    var expression = SyntaxFactory.CSharpExpressionLiteral(new SyntaxList<SyntaxToken>(node.Transition)).WithSpanContext(newContext);

                    return base.VisitCSharpExpressionLiteral(expression);
                }

                _rewriteAsMarkup = true;
                return base.VisitCSharpTransition(node);
            }

            public override SyntaxNode VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
            {
                if (_rewriteAsMarkup)
                {
                    var builder = SyntaxListBuilder<RazorSyntaxNode>.Create();

                    // Convert transition.
                    // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                    var context = node.GetSpanContext();
                    var newContext = new SpanContext(new MarkupChunkGenerator(), context?.EditHandler ?? SpanEditHandler.CreateDefault((content) => Enumerable.Empty<Syntax.InternalSyntax.SyntaxToken>()));

                    var expression = SyntaxFactory.CSharpExpressionLiteral(new SyntaxList<SyntaxToken>(node.Transition.Transition)).WithSpanContext(newContext);
                    expression = (CSharpExpressionLiteralSyntax)VisitCSharpExpressionLiteral(expression);
                    builder.Add(expression);

                    var rewrittenBody = (CSharpCodeBlockSyntax)VisitCSharpCodeBlock(((CSharpImplicitExpressionBodySyntax)node.Body).CSharpCode);
                    builder.AddRange(rewrittenBody.Children);

                    // Since the original transition is part of the body, we need something to take it's place.
                    var transition = SyntaxFactory.CSharpTransition(SyntaxFactory.MissingToken(SyntaxKind.Transition));

                    var rewrittenCodeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
                    return SyntaxFactory.CSharpImplicitExpression(transition, SyntaxFactory.CSharpImplicitExpressionBody(rewrittenCodeBlock));
                }

                return base.VisitCSharpImplicitExpression(node);
            }

            public override SyntaxNode VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
            {
                CSharpTransitionSyntax transition = null;
                var builder = SyntaxListBuilder<RazorSyntaxNode>.Create();
                if (_rewriteAsMarkup)
                {
                    // Convert transition.
                    // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                    var context = node.GetSpanContext();
                    var newContext = new SpanContext(new MarkupChunkGenerator(), context?.EditHandler ?? SpanEditHandler.CreateDefault((content) => Enumerable.Empty<Syntax.InternalSyntax.SyntaxToken>()));

                    var expression = SyntaxFactory.CSharpExpressionLiteral(new SyntaxList<SyntaxToken>(node.Transition.Transition)).WithSpanContext(newContext);
                    expression = (CSharpExpressionLiteralSyntax)VisitCSharpExpressionLiteral(expression);
                    builder.Add(expression);

                    // Since the original transition is part of the body, we need something to take it's place.
                    transition = SyntaxFactory.CSharpTransition(SyntaxFactory.MissingToken(SyntaxKind.Transition));

                    var body = (CSharpExplicitExpressionBodySyntax)node.Body;
                    var rewrittenOpenParen = (RazorSyntaxNode)VisitRazorMetaCode(body.OpenParen);
                    var rewrittenBody = (CSharpCodeBlockSyntax)VisitCSharpCodeBlock(body.CSharpCode);
                    var rewrittenCloseParen = (RazorSyntaxNode)VisitRazorMetaCode(body.CloseParen);
                    builder.Add(rewrittenOpenParen);
                    builder.AddRange(rewrittenBody.Children);
                    builder.Add(rewrittenCloseParen);
                }
                else
                {
                    // This is the first expression of a non-string attribute like attr=@(a + b)
                    // Below code converts this to an implicit expression to make the parens
                    // part of the expression so that it is rendered.
                    transition = (CSharpTransitionSyntax)Visit(node.Transition);
                    var body = (CSharpExplicitExpressionBodySyntax)node.Body;
                    var rewrittenOpenParen = (RazorSyntaxNode)VisitRazorMetaCode(body.OpenParen);
                    var rewrittenBody = (CSharpCodeBlockSyntax)VisitCSharpCodeBlock(body.CSharpCode);
                    var rewrittenCloseParen = (RazorSyntaxNode)VisitRazorMetaCode(body.CloseParen);
                    builder.Add(rewrittenOpenParen);
                    builder.AddRange(rewrittenBody.Children);
                    builder.Add(rewrittenCloseParen);
                }

                var rewrittenCodeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
                return SyntaxFactory.CSharpImplicitExpression(transition, SyntaxFactory.CSharpImplicitExpressionBody(rewrittenCodeBlock));
            }

            public override SyntaxNode VisitRazorMetaCode(RazorMetaCodeSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitRazorMetaCode(node);
                }

                if (_rewriteAsMarkup)
                {
                    // Change to a MarkupChunkGenerator so that the '@' \ parenthesis is generated as part of the output.
                    var context = node.GetSpanContext();
                    var newContext = new SpanContext(new MarkupChunkGenerator(), context.EditHandler);

                    var expression = SyntaxFactory.CSharpExpressionLiteral(new SyntaxList<SyntaxToken>(node.MetaCode)).WithSpanContext(newContext);

                    return VisitCSharpExpressionLiteral(expression);
                }

                _rewriteAsMarkup = true;
                return base.VisitRazorMetaCode(node);
            }

            public override SyntaxNode VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitCSharpExpressionLiteral(node);
                }

                node = (CSharpExpressionLiteralSyntax)ConfigureNonStringAttribute(node);

                _rewriteAsMarkup = true;
                return base.VisitCSharpExpressionLiteral(node);
            }

            public override SyntaxNode VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
            {
                var builder = SyntaxListBuilder<SyntaxToken>.Create();
                if (node.Prefix != null)
                {
                    builder.AddRange(node.Prefix.LiteralTokens);
                }
                if (node.Value != null)
                {
                    builder.AddRange(node.Value.LiteralTokens);
                }

                if (_tryParseResult.IsBoundNonStringAttribute)
                {
                    _rewriteAsMarkup = true;
                    // Since this is a bound non-string attribute, we want to convert LiteralAttributeValue to just be a CSharp Expression literal.
                    var expression = SyntaxFactory.CSharpExpressionLiteral(builder.ToList());
                    return VisitCSharpExpressionLiteral(expression);
                }
                else
                {
                    var literal = SyntaxFactory.MarkupTextLiteral(builder.ToList());
                    var context = node.Value?.GetSpanContext();
                    literal = context != null ? literal.WithSpanContext(context) : literal;

                    return Visit(literal);
                }
            }

            public override SyntaxNode VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
            {
                // Move the prefix to be part of the actual value.
                var builder = SyntaxListBuilder<RazorSyntaxNode>.Create();
                if (node.Prefix != null)
                {
                    builder.Add(node.Prefix);
                }
                if (node.Value?.Children != null)
                {
                    builder.AddRange(node.Value.Children);
                }
                var rewrittenValue = SyntaxFactory.MarkupBlock(builder.ToList());

                return base.VisitMarkupBlock(rewrittenValue);
            }

            public override SyntaxNode VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitCSharpStatementLiteral(node);
                }

                _rewriteAsMarkup = true;
                return base.VisitCSharpStatementLiteral(node);
            }

            public override SyntaxNode VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitMarkupTextLiteral(node);
                }

                _rewriteAsMarkup = true;
                node = (MarkupTextLiteralSyntax)ConfigureNonStringAttribute(node);
                var tokens = new SyntaxList<SyntaxToken>(node.LiteralTokens);
                var value = SyntaxFactory.CSharpExpressionLiteral(tokens);
                return value.WithSpanContext(node.GetSpanContext());
            }

            public override SyntaxNode VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
            {
                if (!_tryParseResult.IsBoundNonStringAttribute)
                {
                    return base.VisitMarkupEphemeralTextLiteral(node);
                }

                // Since this is a non-string attribute we need to rewrite this as code.
                // Rewriting it to CSharpEphemeralTextLiteral so that it is not rendered to output.
                _rewriteAsMarkup = true;
                node = (MarkupEphemeralTextLiteralSyntax)ConfigureNonStringAttribute(node);
                var tokens = new SyntaxList<SyntaxToken>(node.LiteralTokens);
                var value = SyntaxFactory.CSharpEphemeralTextLiteral(tokens);
                return value.WithSpanContext(node.GetSpanContext());
            }

            private SyntaxNode ConfigureNonStringAttribute(SyntaxNode node)
            {
                var context = node.GetSpanContext();
                var builder = context != null ? new SpanContextBuilder(context) : new SpanContextBuilder();
                builder.EditHandler = new ImplicitExpressionEditHandler(
                        builder.EditHandler.Tokenizer,
                        CSharpCodeParser.DefaultKeywords,
                        acceptTrailingDot: true)
                {
                    AcceptedCharacters = AcceptedCharactersInternal.AnyExceptNewline
                };

                if (!_tryParseResult.IsDuplicateAttribute && builder.ChunkGenerator != SpanChunkGenerator.Null)
                {
                    // We want to mark the value of non-string bound attributes to be CSharp.
                    // Except in two cases,
                    // 1. Cases when we don't want to render the span. Eg: Transition span '@'.
                    // 2. Cases when it is a duplicate of a bound attribute. This should just be rendered as html.

                    builder.ChunkGenerator = new ExpressionChunkGenerator();
                }

                context = builder.Build();

                return node.WithSpanContext(context);
            }
        }

        private class TryParseResult
        {
            public string AttributeName { get; set; }

            public RazorSyntaxNode RewrittenAttribute { get; set; }

            public AttributeStructure AttributeStructure { get; set; }

            public bool IsBoundAttribute { get; set; }

            public bool IsBoundNonStringAttribute { get; set; }

            public bool IsBoundBooleanAttribute { get; set; }

            public bool IsMissingDictionaryKey { get; set; }

            public bool IsDuplicateAttribute { get; set; }
        }
    }
}
