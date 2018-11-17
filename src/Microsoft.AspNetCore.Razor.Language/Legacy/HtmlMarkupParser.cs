// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class HtmlMarkupParser : TokenizerBackedParser<HtmlTokenizer>
    {
        private const string ScriptTagName = "script";

        private static readonly SyntaxToken[] nonAllowedHtmlCommentEnding = new[]
        {
            SyntaxFactory.Token(SyntaxKind.Text, "-"),
            SyntaxFactory.Token(SyntaxKind.Bang, "!"),
            SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"),
        };

        private static readonly char[] ValidAfterTypeAttributeNameCharacters = { ' ', '\t', '\r', '\n', '\f', '=' };
        private SourceLocation _lastTagStart = SourceLocation.Zero;
        private SyntaxToken _bufferedOpenAngle;

        //From http://dev.w3.org/html5/spec/Overview.html#elements-0
        private readonly ISet<string> _voidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        public HtmlMarkupParser(ParserContext context)
            : base(context.ParseLeadingDirectives ? FirstDirectiveHtmlLanguageCharacteristics.Instance : HtmlLanguageCharacteristics.Instance, context)
        {
        }

        public CSharpCodeParser CodeParser { get; set; }

        public ISet<string> VoidElements
        {
            get { return _voidElements; }
        }

        private bool CaseSensitive { get; set; }

        private StringComparison Comparison
        {
            get { return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        // Special tags include <!--, <!DOCTYPE, <![CDATA and <? tags
        private bool AtSpecialTag
        {
            get
            {
                if (At(SyntaxKind.OpenAngle))
                {
                    if (NextIs(SyntaxKind.Bang))
                    {
                        return !IsBangEscape(lookahead: 1);
                    }

                    return NextIs(SyntaxKind.QuestionMark);
                }

                return false;
            }
        }

        public RazorDocumentSyntax ParseDocument()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
            }

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            using (PushSpanContextConfig(DefaultMarkupSpanContext))
            {
                var builder = pooledResult.Builder;
                NextToken();
                while (!EndOfFile)
                {
                    SkipToAndParseCode(builder, SyntaxKind.OpenAngle);
                    ParseTagInDocumentContext(builder);
                }
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputAsMarkupLiteral());

                var markup = SyntaxFactory.MarkupBlock(builder.ToList());

                return SyntaxFactory.RazorDocument(markup);
            }
        }

        private void SkipToAndParseCode(in SyntaxListBuilder<RazorSyntaxNode> builder, SyntaxKind type)
        {
            SkipToAndParseCode(builder, token => token.Kind == type);
        }

        private void SkipToAndParseCode(in SyntaxListBuilder<RazorSyntaxNode> builder, Func<SyntaxToken, bool> condition)
        {
            SyntaxToken last = null;
            var startOfLine = false;
            while (!EndOfFile && !condition(CurrentToken))
            {
                if (Context.NullGenerateWhitespaceAndNewLine)
                {
                    Context.NullGenerateWhitespaceAndNewLine = false;
                    SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                    AcceptWhile(token => token.Kind == SyntaxKind.Whitespace);
                    if (At(SyntaxKind.NewLine))
                    {
                        AcceptAndMoveNext();
                    }

                    builder.Add(OutputAsMarkupEphemeralLiteral());
                }
                else if (At(SyntaxKind.NewLine))
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
                else if (At(SyntaxKind.Transition))
                {
                    var transition = CurrentToken;
                    NextToken();
                    if (At(SyntaxKind.Transition))
                    {
                        if (last != null)
                        {
                            Accept(last);
                            last = null;
                        }
                        builder.Add(OutputAsMarkupLiteral());
                        Accept(transition);
                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        builder.Add(OutputAsMarkupEphemeralLiteral());
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
                        if (!Context.DesignTimeMode && last.Kind == SyntaxKind.Whitespace && startOfLine)
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

                    OtherParserBlock(builder);
                }
                else if (At(SyntaxKind.RazorCommentTransition))
                {
                    var shouldRenderWhitespace = true;
                    if (last != null)
                    {
                        // Don't render the whitespace between the start of the line and the razor comment.
                        if (startOfLine && last.Kind == SyntaxKind.Whitespace)
                        {
                            AcceptMarkerTokenIfNecessary();
                            // Output the tokens that may have been accepted prior to the whitespace.
                            builder.Add(OutputAsMarkupLiteral());

                            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                            shouldRenderWhitespace = false;
                        }

                        Accept(last);
                        last = null;
                    }

                    AcceptMarkerTokenIfNecessary();
                    if (shouldRenderWhitespace)
                    {
                        builder.Add(OutputAsMarkupLiteral());
                    }
                    else
                    {
                        builder.Add(OutputAsMarkupEphemeralLiteral());
                    }

                    var comment = ParseRazorComment();
                    builder.Add(comment);

                    // Handle the whitespace and newline at the end of a razor comment.
                    if (startOfLine &&
                        (At(SyntaxKind.NewLine) ||
                        (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.NewLine))))
                    {
                        AcceptWhile(IsSpacingToken(includeNewLines: false));
                        AcceptAndMoveNext();
                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        builder.Add(OutputAsMarkupEphemeralLiteral());
                    }
                }
                else
                {
                    // As long as we see whitespace, we're still at the "start" of the line
                    startOfLine &= At(SyntaxKind.Whitespace);

                    // If there's a last token, accept it
                    if (last != null)
                    {
                        Accept(last);
                        last = null;
                    }

                    // Advance
                    last = CurrentToken;
                    NextToken();
                }
            }

            if (last != null)
            {
                Accept(last);
            }
        }

        /// <summary>
        /// Reads the content of a tag (if present) in the MarkupDocument (or MarkupSection) context,
        /// where we don't care about maintaining a stack of tags.
        /// </summary>
        private void ParseTagInDocumentContext(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (At(SyntaxKind.OpenAngle))
            {
                if (NextIs(SyntaxKind.Bang))
                {
                    // Checking to see if we meet the conditions of a special '!' tag: <!DOCTYPE, <![CDATA[, <!--.
                    if (!IsBangEscape(lookahead: 1))
                    {
                        if (Lookahead(2)?.Kind == SyntaxKind.DoubleHyphen)
                        {
                            builder.Add(OutputAsMarkupLiteral());
                        }

                        AcceptAndMoveNext(); // Accept '<'
                        ParseBangTag(builder);

                        return;
                    }

                    // We should behave like a normal tag that has a parser escape, fall through to the normal
                    // tag logic.
                }
                else if (NextIs(SyntaxKind.QuestionMark))
                {
                    AcceptAndMoveNext(); // Accept '<'
                    TryParseXmlPI(builder);
                    return;
                }

                builder.Add(OutputAsMarkupLiteral());

                // Start tag block
                using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                {
                    var tagBuilder = pooledResult.Builder;
                    AcceptAndMoveNext(); // Accept '<'

                    if (!At(SyntaxKind.ForwardSlash))
                    {
                        ParseOptionalBangEscape(tagBuilder);

                        // Parsing a start tag
                        var scriptTag = At(SyntaxKind.Text) &&
                                        string.Equals(CurrentToken.Content, "script", StringComparison.OrdinalIgnoreCase);
                        TryAccept(SyntaxKind.Text);
                        ParseTagContent(tagBuilder); // Parse the tag, don't care about the content
                        TryAccept(SyntaxKind.ForwardSlash);
                        TryAccept(SyntaxKind.CloseAngle);

                        // If the script tag expects javascript content then we should do minimal parsing until we reach
                        // the end script tag. Don't want to incorrectly parse a "var tag = '<input />';" as an HTML tag.
                        if (scriptTag && !CurrentScriptTagExpectsHtml(tagBuilder))
                        {
                            tagBuilder.Add(OutputAsMarkupLiteral());
                            var block = SyntaxFactory.MarkupTagBlock(tagBuilder.ToList());
                            builder.Add(block);

                            SkipToEndScriptAndParseCode(builder);
                            return;
                        }
                    }
                    else
                    {
                        // Parsing an end tag
                        // This section can accept things like: '</p  >' or '</p>' etc.
                        TryAccept(SyntaxKind.ForwardSlash);

                        // Whitespace here is invalid (according to the spec)
                        ParseOptionalBangEscape(tagBuilder);
                        TryAccept(SyntaxKind.Text);
                        TryAccept(SyntaxKind.Whitespace);
                        TryAccept(SyntaxKind.CloseAngle);
                    }

                    tagBuilder.Add(OutputAsMarkupLiteral());

                    // End tag block
                    var tagBlock = SyntaxFactory.MarkupTagBlock(tagBuilder.ToList());
                    builder.Add(tagBlock);
                }
            }
        }

        private void ParseTagContent(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (!At(SyntaxKind.Whitespace) && !At(SyntaxKind.NewLine))
            {
                // We should be right after the tag name, so if there's no whitespace or new line, something is wrong
                RecoverToEndOfTag(builder);
            }
            else
            {
                // We are here ($): <tag$ foo="bar" biz="~/Baz" />
                while (!EndOfFile && !IsEndOfTag())
                {
                    BeforeAttribute(builder);
                }
            }
        }

        private bool IsEndOfTag()
        {
            if (At(SyntaxKind.ForwardSlash))
            {
                if (NextIs(SyntaxKind.CloseAngle))
                {
                    return true;
                }
                else
                {
                    AcceptAndMoveNext();
                }
            }
            return At(SyntaxKind.CloseAngle) || At(SyntaxKind.OpenAngle);
        }

        private void BeforeAttribute(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // http://dev.w3.org/html5/spec/tokenization.html#before-attribute-name-state
            // Capture whitespace
            var whitespace = ReadWhile(token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);

            if (At(SyntaxKind.Transition) || At(SyntaxKind.RazorCommentTransition))
            {
                // Transition outside of attribute value => Switch to recovery mode
                Accept(whitespace);
                RecoverToEndOfTag(builder);
                return;
            }

            // http://dev.w3.org/html5/spec/tokenization.html#attribute-name-state
            // Read the 'name' (i.e. read until the '=' or whitespace/newline)
            var nameTokens = Enumerable.Empty<SyntaxToken>();
            var whitespaceAfterAttributeName = Enumerable.Empty<SyntaxToken>();
            if (IsValidAttributeNameToken(CurrentToken))
            {
                nameTokens = ReadWhile(token =>
                                 token.Kind != SyntaxKind.Whitespace &&
                                 token.Kind != SyntaxKind.NewLine &&
                                 token.Kind != SyntaxKind.Equals &&
                                 token.Kind != SyntaxKind.CloseAngle &&
                                 token.Kind != SyntaxKind.OpenAngle &&
                                 (token.Kind != SyntaxKind.ForwardSlash || !NextIs(SyntaxKind.CloseAngle)));

                // capture whitespace after attribute name (if any)
                whitespaceAfterAttributeName = ReadWhile(
                    token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);
            }
            else
            {
                // Unexpected character in tag, enter recovery
                Accept(whitespace);
                RecoverToEndOfTag(builder);
                return;
            }

            if (!At(SyntaxKind.Equals))
            {
                // Minimized attribute

                // We are at the prefix of the next attribute or the end of tag. Put it back so it is parsed later.
                PutCurrentBack();
                PutBack(whitespaceAfterAttributeName);

                // Output anything prior to the attribute, in most cases this will be the tag name:
                // |<input| checked />. If in-between other attributes this will noop or output malformed attribute
                // content (if the previous attribute was malformed).
                builder.Add(OutputAsMarkupLiteral());

                Accept(whitespace);
                var namePrefix = OutputAsMarkupLiteral();
                Accept(nameTokens);
                var name = OutputAsMarkupLiteral();

                var minimizedAttributeBlock = SyntaxFactory.MarkupMinimizedAttributeBlock(namePrefix, name);
                builder.Add(minimizedAttributeBlock);

                return;
            }

            // Not a minimized attribute, parse as if it were well-formed (if attribute turns out to be malformed we
            // will go into recovery).
            builder.Add(OutputAsMarkupLiteral());

            var attributeBlock = ParseAttributePrefix(whitespace, nameTokens, whitespaceAfterAttributeName);

            builder.Add(attributeBlock);
        }

        private MarkupAttributeBlockSyntax ParseAttributePrefix(
            IEnumerable<SyntaxToken> whitespace,
            IEnumerable<SyntaxToken> nameTokens,
            IEnumerable<SyntaxToken> whitespaceAfterAttributeName)
        {
            // First, determine if this is a 'data-' attribute (since those can't use conditional attributes)
            var nameContent = string.Concat(nameTokens.Select(s => s.Content));
            var attributeCanBeConditional =
                Context.FeatureFlags.EXPERIMENTAL_AllowConditionalDataDashAttributes ||
                !nameContent.StartsWith("data-", StringComparison.OrdinalIgnoreCase);

            // Accept the whitespace and name
            Accept(whitespace);
            var namePrefix = OutputAsMarkupLiteral();
            Accept(nameTokens);
            var name = OutputAsMarkupLiteral();

            // Since this is not a minimized attribute, the whitespace after attribute name belongs to this attribute.
            Accept(whitespaceAfterAttributeName);
            var nameSuffix = OutputAsMarkupLiteral();
            Assert(SyntaxKind.Equals); // We should be at "="
            var equalsToken = EatCurrentToken();

            var whitespaceAfterEquals = ReadWhile(token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);
            var quote = SyntaxKind.Marker;
            if (At(SyntaxKind.SingleQuote) || At(SyntaxKind.DoubleQuote))
            {
                // Found a quote, the whitespace belongs to this attribute.
                Accept(whitespaceAfterEquals);
                quote = CurrentToken.Kind;
                AcceptAndMoveNext();
            }
            else if (whitespaceAfterEquals.Any())
            {
                // No quotes found after the whitespace. Put it back so that it can be parsed later.
                PutCurrentBack();
                PutBack(whitespaceAfterEquals);
            }

            MarkupTextLiteralSyntax valuePrefix = null;
            RazorBlockSyntax attributeValue = null;
            MarkupTextLiteralSyntax valueSuffix = null;

            if (attributeCanBeConditional)
            {
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null; // The block chunk generator will render the prefix

                // We now have the value prefix which is usually whitespace and/or a quote
                valuePrefix = OutputAsMarkupLiteral();

                // Read the attribute value only if the value is quoted
                // or if there is no whitespace between '=' and the unquoted value.
                if (quote != SyntaxKind.Marker || !whitespaceAfterEquals.Any())
                {
                    using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                    {
                        var attributeValueBuilder = pooledResult.Builder;
                        // Read the attribute value.
                        while (!EndOfFile && !IsEndOfAttributeValue(quote, CurrentToken))
                        {
                            ParseAttributeValue(attributeValueBuilder, quote);
                        }

                        if (attributeValueBuilder.Count > 0)
                        {
                            attributeValue = SyntaxFactory.GenericBlock(attributeValueBuilder.ToList());
                        }
                    }
                }

                // Capture the suffix
                if (quote != SyntaxKind.Marker && At(quote))
                {
                    AcceptAndMoveNext();
                    // Again, block chunk generator will render the suffix
                    SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                    valueSuffix = OutputAsMarkupLiteral();
                }
            }
            else if (quote != SyntaxKind.Marker || !whitespaceAfterEquals.Any())
            {
                valuePrefix = OutputAsMarkupLiteral();

                using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                {
                    var attributeValueBuilder = pooledResult.Builder;
                    // Not a "conditional" attribute, so just read the value
                    SkipToAndParseCode(attributeValueBuilder, token => IsEndOfAttributeValue(quote, token));

                    // Output already accepted tokens if any as markup literal
                    var literalValue = OutputAsMarkupLiteral();
                    attributeValueBuilder.Add(literalValue);

                    // Capture the attribute value (will include everything in-between the attribute's quotes).
                    attributeValue = SyntaxFactory.GenericBlock(attributeValueBuilder.ToList());
                }

                if (quote != SyntaxKind.Marker)
                {
                    TryAccept(quote);
                    valueSuffix = OutputAsMarkupLiteral();
                }
            }
            else
            {
                // There is no quote and there is whitespace after equals. There is no attribute value.
            }

            return SyntaxFactory.MarkupAttributeBlock(namePrefix, name, nameSuffix, equalsToken, valuePrefix, attributeValue, valueSuffix);
        }

        private void ParseAttributeValue(in SyntaxListBuilder<RazorSyntaxNode> builder, SyntaxKind quote)
        {
            var prefixStart = CurrentStart;
            var prefixTokens = ReadWhile(token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);

            if (At(SyntaxKind.Transition))
            {
                if (NextIs(SyntaxKind.Transition))
                {
                    // Wrapping this in a block so that the ConditionalAttributeCollapser doesn't rewrite it.
                    using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                    {
                        var markupBuilder = pooledResult.Builder;
                        Accept(prefixTokens);

                        // Render a single "@" in place of "@@".
                        SpanContext.ChunkGenerator = new LiteralAttributeChunkGenerator(
                            new LocationTagged<string>(string.Concat(prefixTokens.Select(s => s.Content)), prefixStart),
                            new LocationTagged<string>(CurrentToken.Content, CurrentStart));
                        AcceptAndMoveNext();
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                        markupBuilder.Add(OutputAsMarkupLiteral());

                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        AcceptAndMoveNext();
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                        markupBuilder.Add(OutputAsMarkupEphemeralLiteral());

                        var markupBlock = SyntaxFactory.MarkupBlock(markupBuilder.ToList());
                        builder.Add(markupBlock);
                    }
                }
                else
                {
                    Accept(prefixTokens);
                    var valueStart = CurrentStart;
                    PutCurrentBack();

                    var prefix = OutputAsMarkupLiteral();

                    // Dynamic value, start a new block and set the chunk generator
                    using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                    {
                        var dynamicAttributeValueBuilder = pooledResult.Builder;

                        OtherParserBlock(dynamicAttributeValueBuilder);
                        var value = SyntaxFactory.MarkupDynamicAttributeValue(prefix, SyntaxFactory.GenericBlock(dynamicAttributeValueBuilder.ToList()));
                        builder.Add(value);
                    }
                }
            }
            else
            {
                Accept(prefixTokens);
                var prefix = OutputAsMarkupLiteral();

                // Literal value
                // 'quote' should be "Unknown" if not quoted and tokens coming from the tokenizer should never have
                // "Unknown" type.
                var valueTokens = ReadWhile(token =>
                    // These three conditions find separators which break the attribute value into portions
                    token.Kind != SyntaxKind.Whitespace &&
                    token.Kind != SyntaxKind.NewLine &&
                    token.Kind != SyntaxKind.Transition &&
                    // This condition checks for the end of the attribute value (it repeats some of the checks above
                    // but for now that's ok)
                    !IsEndOfAttributeValue(quote, token));
                Accept(valueTokens);
                var value = OutputAsMarkupLiteral();

                var literalAttributeValue = SyntaxFactory.MarkupLiteralAttributeValue(prefix, value);
                builder.Add(literalAttributeValue);
            }
        }

        private void RecoverToEndOfTag(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Accept until ">", "/" or "<", but parse code
            while (!EndOfFile)
            {
                SkipToAndParseCode(builder, IsTagRecoveryStopPoint);
                if (!EndOfFile)
                {
                    EnsureCurrent();
                    switch (CurrentToken.Kind)
                    {
                        case SyntaxKind.SingleQuote:
                        case SyntaxKind.DoubleQuote:
                            ParseQuoted(builder);
                            break;
                        case SyntaxKind.OpenAngle:
                        // Another "<" means this tag is invalid.
                        case SyntaxKind.ForwardSlash:
                        // Empty tag
                        case SyntaxKind.CloseAngle:
                            // End of tag
                            return;
                        default:
                            AcceptAndMoveNext();
                            break;
                    }
                }
            }
        }

        private bool IsTagRecoveryStopPoint(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.CloseAngle ||
                   token.Kind == SyntaxKind.ForwardSlash ||
                   token.Kind == SyntaxKind.OpenAngle ||
                   token.Kind == SyntaxKind.SingleQuote ||
                   token.Kind == SyntaxKind.DoubleQuote;
        }

        private bool IsEndOfAttributeValue(SyntaxKind quote, SyntaxToken token)
        {
            return EndOfFile || token == null ||
                   (quote != SyntaxKind.Marker
                        ? token.Kind == quote // If quoted, just wait for the quote
                        : IsUnquotedEndOfAttributeValue(token));
        }

        private bool IsUnquotedEndOfAttributeValue(SyntaxToken token)
        {
            // If unquoted, we have a larger set of terminating characters:
            // http://dev.w3.org/html5/spec/tokenization.html#attribute-value-unquoted-state
            // Also we need to detect "/" and ">"
            return token.Kind == SyntaxKind.DoubleQuote ||
                   token.Kind == SyntaxKind.SingleQuote ||
                   token.Kind == SyntaxKind.OpenAngle ||
                   token.Kind == SyntaxKind.Equals ||
                   (token.Kind == SyntaxKind.ForwardSlash && NextIs(SyntaxKind.CloseAngle)) ||
                   token.Kind == SyntaxKind.CloseAngle ||
                   token.Kind == SyntaxKind.Whitespace ||
                   token.Kind == SyntaxKind.NewLine;
        }

        private void ParseQuoted(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            var type = CurrentToken.Kind;
            AcceptAndMoveNext();
            SkipToAndParseCode(builder, type);
            if (!EndOfFile)
            {
                Assert(type);
                AcceptAndMoveNext();
            }
        }

        private bool ParseBangTag(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Accept "!"
            Assert(SyntaxKind.Bang);

            if (AcceptAndMoveNext())
            {
                if (IsHtmlCommentAhead())
                {
                    using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                    {
                        var htmlCommentBuilder = pooledResult.Builder;

                        // Accept the double-hyphen token at the beginning of the comment block.
                        AcceptAndMoveNext();
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                        htmlCommentBuilder.Add(OutputAsMarkupLiteral());

                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;
                        while (!EndOfFile)
                        {
                            SkipToAndParseCode(htmlCommentBuilder, SyntaxKind.DoubleHyphen);
                            var lastDoubleHyphen = AcceptAllButLastDoubleHyphens();

                            if (At(SyntaxKind.CloseAngle))
                            {
                                // Output the content in the comment block as a separate markup
                                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;
                                htmlCommentBuilder.Add(OutputAsMarkupLiteral());

                                // This is the end of a comment block
                                Accept(lastDoubleHyphen);
                                AcceptAndMoveNext();
                                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                                htmlCommentBuilder.Add(OutputAsMarkupLiteral());
                                var commentBlock = SyntaxFactory.MarkupCommentBlock(htmlCommentBuilder.ToList());
                                builder.Add(commentBlock);
                                return true;
                            }
                            else if (lastDoubleHyphen != null)
                            {
                                Accept(lastDoubleHyphen);
                            }
                        }
                    }
                }
                else if (CurrentToken.Kind == SyntaxKind.LeftBracket)
                {
                    if (AcceptAndMoveNext())
                    {
                        return TryParseCData(builder);
                    }
                }
                else
                {
                    AcceptAndMoveNext();
                    return AcceptTokenUntilAll(builder, SyntaxKind.CloseAngle);
                }
            }

            return false;
        }

        internal static bool IsHyphen(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.Text && token.Content == "-";
        }

        protected bool IsHtmlCommentAhead()
        {
            // From HTML5 Specification, available at http://www.w3.org/TR/html52/syntax.html#comments

            // Comments must have the following format:
            // 1. The string "<!--"
            // 2. Optionally, text, with the additional restriction that the text
            //      2.1 must not start with the string ">" nor start with the string "->"
            //      2.2 nor contain the strings
            //          2.2.1 "<!--"
            //          2.2.2 "-->" As we will be treating this as a comment ending, there is no need to handle this case at all.
            //          2.2.3 "--!>"
            //      2.3 nor end with the string "<!-".
            // 3. The string "-->"

            if (CurrentToken.Kind != SyntaxKind.DoubleHyphen)
            {
                return false;
            }

            // Check condition 2.1
            if (NextIs(SyntaxKind.CloseAngle) || NextIs(next => IsHyphen(next) && NextIs(SyntaxKind.CloseAngle)))
            {
                return false;
            }

            // Check condition 2.2
            var isValidComment = false;
            LookaheadUntil((token, prevTokens) =>
            {
                if (token.Kind == SyntaxKind.DoubleHyphen)
                {
                    if (NextIs(SyntaxKind.CloseAngle))
                    {
                        // Check condition 2.3: We're at the end of a comment. Check to make sure the text ending is allowed.
                        isValidComment = !IsCommentContentEndingInvalid(prevTokens);
                        return true;
                    }
                    else if (NextIs(ns => IsHyphen(ns) && NextIs(SyntaxKind.CloseAngle)))
                    {
                        // Check condition 2.3: we're at the end of a comment, which has an extra dash.
                        // Need to treat the dash as part of the content and check the ending.
                        // However, that case would have already been checked as part of check from 2.2.1 which
                        // would already fail this iteration and we wouldn't get here
                        isValidComment = true;
                        return true;
                    }
                    else if (NextIs(ns => ns.Kind == SyntaxKind.Bang && NextIs(SyntaxKind.CloseAngle)))
                    {
                        // This is condition 2.2.3
                        isValidComment = false;
                        return true;
                    }
                }
                else if (token.Kind == SyntaxKind.OpenAngle)
                {
                    // Checking condition 2.2.1
                    if (NextIs(ns => ns.Kind == SyntaxKind.Bang && NextIs(SyntaxKind.DoubleHyphen)))
                    {
                        isValidComment = false;
                        return true;
                    }
                }

                return false;
            });

            return isValidComment;
        }

        /// <summary>
        /// Verifies, that the sequence doesn't end with the "&lt;!-" HtmlTokens. Note, the first token is an opening bracket token
        /// </summary>
        internal static bool IsCommentContentEndingInvalid(IEnumerable<SyntaxToken> sequence)
        {
            var reversedSequence = sequence.Reverse();
            var index = 0;
            foreach (var item in reversedSequence)
            {
                if (!item.IsEquivalentTo(nonAllowedHtmlCommentEnding[index++]))
                {
                    return false;
                }

                if (index == nonAllowedHtmlCommentEnding.Length)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryParseCData(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (CurrentToken.Kind == SyntaxKind.Text && string.Equals(CurrentToken.Content, "cdata", StringComparison.OrdinalIgnoreCase))
            {
                if (AcceptAndMoveNext())
                {
                    if (CurrentToken.Kind == SyntaxKind.LeftBracket)
                    {
                        return AcceptTokenUntilAll(builder, SyntaxKind.RightBracket, SyntaxKind.RightBracket, SyntaxKind.CloseAngle);
                    }
                }
            }

            return false;
        }

        private bool TryParseXmlPI(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Accept "?"
            Assert(SyntaxKind.QuestionMark);
            AcceptAndMoveNext();
            return AcceptTokenUntilAll(builder, SyntaxKind.QuestionMark, SyntaxKind.CloseAngle);
        }

        private void ParseOptionalBangEscape(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (IsBangEscape(lookahead: 0))
            {
                builder.Add(OutputAsMarkupLiteral());

                // Accept the parser escape character '!'.
                Assert(SyntaxKind.Bang);
                AcceptAndMoveNext();

                // Setup the metacode span that we will be outputing.
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                builder.Add(OutputAsMetaCode(Output()));
            }
        }

        private bool IsBangEscape(int lookahead)
        {
            var potentialBang = Lookahead(lookahead);

            if (potentialBang != null &&
                potentialBang.Kind == SyntaxKind.Bang)
            {
                var afterBang = Lookahead(lookahead + 1);

                return afterBang != null &&
                    afterBang.Kind == SyntaxKind.Text &&
                    !string.Equals(afterBang.Content, "DOCTYPE", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void SkipToEndScriptAndParseCode(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal endTagAcceptedCharacters = AcceptedCharactersInternal.Any)
        {
            // Special case for <script>: Skip to end of script tag and parse code
            var seenEndScript = false;

            while (!seenEndScript && !EndOfFile)
            {
                SkipToAndParseCode(builder, SyntaxKind.OpenAngle);
                var tagStart = CurrentStart;

                if (NextIs(SyntaxKind.ForwardSlash))
                {
                    var openAngle = CurrentToken;
                    NextToken(); // Skip over '<', current is '/'
                    var solidus = CurrentToken;
                    NextToken(); // Skip over '/', current should be text

                    if (At(SyntaxKind.Text) &&
                        string.Equals(CurrentToken.Content, ScriptTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        seenEndScript = true;
                    }

                    // We put everything back because we just wanted to look ahead to see if the current end tag that we're parsing is
                    // the script tag.  If so we'll generate correct code to encompass it.
                    PutCurrentBack(); // Put back whatever was after the solidus
                    PutBack(solidus); // Put back '/'
                    PutBack(openAngle); // Put back '<'

                    // We just looked ahead, this NextToken will set CurrentToken to an open angle bracket.
                    NextToken();
                }

                if (seenEndScript)
                {
                    builder.Add(OutputAsMarkupLiteral());

                    using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                    {
                        var tagBuilder = pooledResult.Builder;
                        SpanContext.EditHandler.AcceptedCharacters = endTagAcceptedCharacters;

                        AcceptAndMoveNext(); // '<'
                        AcceptAndMoveNext(); // '/'
                        SkipToAndParseCode(tagBuilder, SyntaxKind.CloseAngle);
                        if (!TryAccept(SyntaxKind.CloseAngle))
                        {
                            Context.ErrorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                                    new SourceSpan(SourceLocationTracker.Advance(tagStart, "</"), ScriptTagName.Length),
                                    ScriptTagName));
                            var closeAngle = SyntaxFactory.MissingToken(SyntaxKind.CloseAngle);
                            Accept(closeAngle);
                        }
                        tagBuilder.Add(OutputAsMarkupLiteral());
                        builder.Add(SyntaxFactory.MarkupTagBlock(tagBuilder.ToList()));
                    }
                }
                else
                {
                    AcceptAndMoveNext(); // Accept '<' (not the closing script tag's open angle)
                }
            }
        }

        private bool CurrentScriptTagExpectsHtml(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            Debug.Assert(!builder.IsNull);

            MarkupAttributeBlockSyntax typeAttribute = null;
            for (var i = 0; i < builder.Count; i++)
            {
                var node = builder[i];
                if (node.IsToken || node.IsTrivia)
                {
                    continue;
                }

                if (node is MarkupAttributeBlockSyntax attributeBlock &&
                    attributeBlock.Value.Children.Count > 0 &&
                    IsTypeAttribute(attributeBlock))
                {
                    typeAttribute = attributeBlock;
                    break;
                }
            }

            if (typeAttribute != null)
            {
                var contentValues = typeAttribute.Value.CreateRed().DescendantNodes().Where(n => n.IsToken).Cast<Syntax.SyntaxToken>();

                var scriptType = string.Concat(contentValues.Select(t => t.Content)).Trim();

                // Does not allow charset parameter (or any other parameters).
                return string.Equals(scriptType, "text/html", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool IsTypeAttribute(MarkupAttributeBlockSyntax attributeBlock)
        {
            if (attributeBlock.Name.LiteralTokens.Count == 0)
            {
                return false;
            }

            var trimmedStartContent = attributeBlock.Name.ToFullString().TrimStart();
            if (trimmedStartContent.StartsWith("type", StringComparison.OrdinalIgnoreCase) &&
                (trimmedStartContent.Length == 4 ||
                ValidAfterTypeAttributeNameCharacters.Contains(trimmedStartContent[4])))
            {
                return true;
            }

            return false;
        }

        protected SyntaxToken AcceptAllButLastDoubleHyphens()
        {
            var lastDoubleHyphen = CurrentToken;
            AcceptWhile(s =>
            {
                if (NextIs(SyntaxKind.DoubleHyphen))
                {
                    lastDoubleHyphen = s;
                    return true;
                }

                return false;
            });

            NextToken();

            if (At(SyntaxKind.Text) && IsHyphen(CurrentToken))
            {
                // Doing this here to maintain the order of tokens
                if (!NextIs(SyntaxKind.CloseAngle))
                {
                    Accept(lastDoubleHyphen);
                    lastDoubleHyphen = null;
                }

                AcceptAndMoveNext();
            }

            return lastDoubleHyphen;
        }

        private bool AcceptTokenUntilAll(in SyntaxListBuilder<RazorSyntaxNode> builder, params SyntaxKind[] endSequence)
        {
            while (!EndOfFile)
            {
                SkipToAndParseCode(builder, endSequence[0]);
                if (AcceptAll(endSequence))
                {
                    return true;
                }
            }
            Debug.Assert(EndOfFile);
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
            return false;
        }

        public MarkupBlockSyntax ParseBlock()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
            }

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            using (PushSpanContextConfig(DefaultMarkupSpanContext))
            {
                var builder = pooledResult.Builder;
                if (!NextToken())
                {
                    return null;
                }

                AcceptWhile(IsSpacingToken(includeNewLines: true));

                if (CurrentToken.Kind == SyntaxKind.OpenAngle)
                {
                    // "<" => Implicit Tag Block
                    ParseTagBlock(builder, new Stack<Tuple<SyntaxToken, SourceLocation>>());
                }
                else if (CurrentToken.Kind == SyntaxKind.Transition)
                {
                    // "@" => Explicit Tag/Single Line Block OR Template

                    // Output whitespace
                    builder.Add(OutputAsMarkupLiteral());

                    // Definitely have a transition span
                    Assert(SyntaxKind.Transition);
                    AcceptAndMoveNext();
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                    SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                    var transition = GetNodeWithSpanContext(SyntaxFactory.MarkupTransition(Output()));
                    builder.Add(transition);
                    if (At(SyntaxKind.Transition))
                    {
                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        AcceptAndMoveNext();
                        builder.Add(OutputAsMetaCode(Output(), AcceptedCharactersInternal.Any));
                    }
                    ParseAfterTransition(builder);
                }
                else
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_MarkupBlockMustStartWithTag(
                            new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                }
                builder.Add(OutputAsMarkupLiteral());

                var markupBlock = builder.ToList();

                return SyntaxFactory.MarkupBlock(markupBlock);
            }
        }

        private void ParseAfterTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // "@:" => Explicit Single Line Block
            if (CurrentToken.Kind == SyntaxKind.Text && CurrentToken.Content.Length > 0 && CurrentToken.Content[0] == ':')
            {
                // Split the token
                var split = Language.SplitToken(CurrentToken, 1, SyntaxKind.Colon);

                // The first part (left) is added to this span and we return a MetaCode span
                Accept(split.Item1);
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                builder.Add(OutputAsMetaCode(Output(), AcceptedCharactersInternal.Any));
                if (split.Item2 != null)
                {
                    Accept(split.Item2);
                }
                NextToken();
                ParseSingleLineMarkup(builder);
            }
            else if (CurrentToken.Kind == SyntaxKind.OpenAngle)
            {
                ParseTagBlock(builder, new Stack<Tuple<SyntaxToken, SourceLocation>>());
            }
        }

        private void ParseSingleLineMarkup(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Parse until a newline, it's that simple!
            // First, signal to code parser that whitespace is significant to us.
            var old = Context.WhiteSpaceIsSignificantToAncestorBlock;
            Context.WhiteSpaceIsSignificantToAncestorBlock = true;
            SpanContext.EditHandler = new SpanEditHandler(Language.TokenizeString);
            SkipToAndParseCode(builder, SyntaxKind.NewLine);
            if (!EndOfFile && CurrentToken.Kind == SyntaxKind.NewLine)
            {
                AcceptAndMoveNext();
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }
            PutCurrentBack();
            Context.WhiteSpaceIsSignificantToAncestorBlock = old;
            builder.Add(OutputAsMarkupLiteral());
        }

        private void ParseTagBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Stack<Tuple<SyntaxToken, SourceLocation>> tags)
        {
            // TODO: This is really ugly and needs to be cleaned up.

            // Skip Whitespace and Text
            var completeTag = false;
            var blockAlreadyBuilt = false;
            do
            {
                SkipToAndParseCode(builder, SyntaxKind.OpenAngle);

                // Output everything prior to the OpenAngle into a markup span
                builder.Add(OutputAsMarkupLiteral());

                var tagBuilder = builder;
                IDisposable tagBuilderDisposable = null;
                try
                {
                    if (EndOfFile)
                    {
                        // Do not want to start a new tag block if we're at the end of the file.
                        EndTagBlock(builder, tags, complete: true);
                    }
                    else
                    {
                        var atSpecialTag = AtSpecialTag;
                        if (!atSpecialTag)
                        {
                            // Start a tag block.  This is used to wrap things like <p> or <a class="btn"> etc.
                            var pooledResult = Pool.Allocate<RazorSyntaxNode>();
                            tagBuilderDisposable = pooledResult;
                            tagBuilder = pooledResult.Builder;
                        }
                        _bufferedOpenAngle = null;
                        _lastTagStart = CurrentStart;
                        Assert(SyntaxKind.OpenAngle);
                        _bufferedOpenAngle = CurrentToken;
                        var tagStart = CurrentStart;
                        if (!NextToken())
                        {
                            Accept(_bufferedOpenAngle);
                            EndTagBlock(tagBuilder, tags, complete: false);
                        }
                        else if (atSpecialTag && At(SyntaxKind.Bang))
                        {
                            Accept(_bufferedOpenAngle);
                            completeTag = ParseBangTag(builder);
                            blockAlreadyBuilt = completeTag;
                        }
                        else
                        {
                            var result = ParseAfterTagStart(tagBuilder, builder, tagStart, tags);
                            completeTag = result.Item1;
                            blockAlreadyBuilt = result.Item2;
                        }
                    }

                    if (completeTag)
                    {
                        // Completed tags have no accepted characters inside of blocks.
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                    }

                    if (blockAlreadyBuilt)
                    {
                        // Output the contents of the tag into its own markup span.
                        builder.Add(OutputAsMarkupLiteral());
                    }
                    else if (tagBuilderDisposable != null)
                    {
                        // A new tag block was started. Build it.
                        // Output the contents of the tag into its own markup span.
                        tagBuilder.Add(OutputAsMarkupLiteral());
                        var tagBlock = SyntaxFactory.MarkupTagBlock(tagBuilder.ToList());
                        builder.Add(tagBlock);
                    }
                }
                finally
                {
                    // Will be null if we were at end of file or special tag when initially created.
                    if (tagBuilderDisposable != null)
                    {
                        // End tag block
                        tagBuilderDisposable.Dispose();
                    }
                }
            }
            while (tags.Count > 0);

            EndTagBlock(builder, tags, completeTag);
        }

        private Tuple<bool, bool> ParseAfterTagStart(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            in SyntaxListBuilder<RazorSyntaxNode> parentBuilder,
            SourceLocation tagStart,
            Stack<Tuple<SyntaxToken, SourceLocation>> tags)
        {
            var blockAlreadyBuilt = false;
            if (!EndOfFile)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.ForwardSlash:
                        // End Tag
                        return ParseEndTag(builder, parentBuilder, tagStart, tags);
                    case SyntaxKind.QuestionMark:
                        // XML PI
                        Accept(_bufferedOpenAngle);
                        var complete = TryParseXmlPI(builder);
                        // No block is created for Xml PI. So return the same value as complete.
                        blockAlreadyBuilt = complete;
                        return Tuple.Create(complete, blockAlreadyBuilt);
                    default:
                        // Start Tag
                        return ParseStartTag(builder, parentBuilder, tags);
                }
            }
            if (tags.Count == 0)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_OuterTagMissingName(
                        new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
            }

            return Tuple.Create(false, blockAlreadyBuilt);
        }

        private Tuple<bool, bool> ParseStartTag(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            in SyntaxListBuilder<RazorSyntaxNode> parentBuilder,
            Stack<Tuple<SyntaxToken, SourceLocation>> tags)
        {
            SyntaxToken bangToken = null;
            SyntaxToken potentialTagNameToken;

            if (At(SyntaxKind.Bang))
            {
                bangToken = CurrentToken;

                potentialTagNameToken = Lookahead(count: 1);
            }
            else
            {
                potentialTagNameToken = CurrentToken;
            }

            SyntaxToken tagName;

            if (potentialTagNameToken == null || potentialTagNameToken.Kind != SyntaxKind.Text)
            {
                tagName = SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);
            }
            else if (bangToken != null)
            {
                tagName = SyntaxFactory.Token(SyntaxKind.Text, "!" + potentialTagNameToken.Content);
            }
            else
            {
                tagName = potentialTagNameToken;
            }

            var tag = Tuple.Create(tagName, _lastTagStart);

            if (tags.Count == 0 &&
                // Note tagName may contain a '!' escape character. This ensures <!text> doesn't match here.
                // <!text> tags are treated like any other escaped HTML start tag.
                string.Equals(tag.Item1.Content, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(OutputAsMarkupLiteral());
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;

                Accept(_bufferedOpenAngle);
                var textLocation = CurrentStart;
                Assert(SyntaxKind.Text);

                AcceptAndMoveNext();

                var bookmark = CurrentStart.AbsoluteIndex;
                var tokens = ReadWhile(IsSpacingToken(includeNewLines: true));
                var empty = At(SyntaxKind.ForwardSlash);
                if (empty)
                {
                    Accept(tokens);
                    Assert(SyntaxKind.ForwardSlash);
                    AcceptAndMoveNext();
                    bookmark = CurrentStart.AbsoluteIndex;
                    tokens = ReadWhile(IsSpacingToken(includeNewLines: true));
                }

                if (!TryAccept(SyntaxKind.CloseAngle))
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                            new SourceSpan(textLocation, contentLength: 4 /* text */)));

                    RecoverTextTag();
                }
                else
                {
                    Accept(tokens);
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                }

                if (!empty)
                {
                    tags.Push(tag);
                }

                var transition = GetNodeWithSpanContext(SyntaxFactory.MarkupTransition(Output()));
                builder.Add(transition);
                var tagBlock = SyntaxFactory.MarkupTagBlock(builder.ToList());
                parentBuilder.Add(tagBlock);

                return Tuple.Create(true, true);
            }

            Accept(_bufferedOpenAngle);
            ParseOptionalBangEscape(builder);
            TryAccept(SyntaxKind.Text);
            return ParseRestOfTag(builder, parentBuilder, tag, tags);
        }

        private Tuple<bool, bool> ParseRestOfTag(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            in SyntaxListBuilder<RazorSyntaxNode> parentBuilder,
            Tuple<SyntaxToken, SourceLocation> tag,
            Stack<Tuple<SyntaxToken, SourceLocation>> tags)
        {
            var blockAlreadyBuilt = false;
            ParseTagContent(builder);

            // We are now at a possible end of the tag
            // Found '<', so we just abort this tag.
            if (At(SyntaxKind.OpenAngle))
            {
                return Tuple.Create(false, blockAlreadyBuilt);
            }

            var isEmpty = At(SyntaxKind.ForwardSlash);
            // Found a solidus, so don't accept it but DON'T push the tag to the stack
            if (isEmpty)
            {
                AcceptAndMoveNext();
            }

            // Check for the '>' to determine if the tag is finished
            var seenClose = TryAccept(SyntaxKind.CloseAngle);
            if (!seenClose)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                        new SourceSpan(
                            SourceLocationTracker.Advance(tag.Item2, "<"),
                            Math.Max(tag.Item1.Content.Length, 1)),
                        tag.Item1.Content));
            }
            else
            {
                if (!isEmpty)
                {
                    // Is this a void element?
                    var tagName = tag.Item1.Content.Trim();
                    if (VoidElements.Contains(tagName))
                    {
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                        builder.Add(OutputAsMarkupLiteral());
                        var tagBlock = SyntaxFactory.MarkupTagBlock(builder.ToList());
                        parentBuilder.Add(tagBlock);
                        blockAlreadyBuilt = true;

                        // Technically, void elements like "meta" are not allowed to have end tags. Just in case they do,
                        // we need to look ahead at the next set of tokens. If we see "<", "/", tag name, accept it and the ">" following it
                        // Place a bookmark
                        var bookmark = CurrentStart.AbsoluteIndex;

                        // Skip whitespace
                        var whiteSpace = ReadWhile(IsSpacingToken(includeNewLines: true));

                        // Open Angle
                        if (At(SyntaxKind.OpenAngle) && NextIs(SyntaxKind.ForwardSlash))
                        {
                            var openAngle = CurrentToken;
                            NextToken();
                            Assert(SyntaxKind.ForwardSlash);
                            var solidus = CurrentToken;
                            NextToken();
                            if (At(SyntaxKind.Text) && string.Equals(CurrentToken.Content, tagName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Accept up to here
                                Accept(whiteSpace);
                                parentBuilder.Add(OutputAsMarkupLiteral()); // Output the whitespace

                                using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                                {
                                    var tagBuilder = pooledResult.Builder;
                                    Accept(openAngle);
                                    Accept(solidus);
                                    AcceptAndMoveNext();

                                    // Accept to '>', '<' or EOF
                                    AcceptUntil(SyntaxKind.CloseAngle, SyntaxKind.OpenAngle);
                                    // Accept the '>' if we saw it. And if we do see it, we're complete
                                    var complete = TryAccept(SyntaxKind.CloseAngle);

                                    if (complete)
                                    {
                                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                                    }

                                    // Output the closing void element
                                    tagBuilder.Add(OutputAsMarkupLiteral());
                                    parentBuilder.Add(SyntaxFactory.MarkupTagBlock(tagBuilder.ToList()));

                                    return Tuple.Create(complete, blockAlreadyBuilt);
                                }
                            }
                        }

                        // Go back to the bookmark and just finish this tag at the close angle
                        Context.Source.Position = bookmark;
                        NextToken();
                    }
                    else if (string.Equals(tagName, ScriptTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!CurrentScriptTagExpectsHtml(builder))
                        {
                            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                            builder.Add(OutputAsMarkupLiteral());
                            var tagBlock = SyntaxFactory.MarkupTagBlock(builder.ToList());
                            parentBuilder.Add(tagBlock);
                            blockAlreadyBuilt = true;

                            SkipToEndScriptAndParseCode(parentBuilder, endTagAcceptedCharacters: AcceptedCharactersInternal.None);
                        }
                        else
                        {
                            // Push the script tag onto the tag stack, it should be treated like all other HTML tags.
                            tags.Push(tag);
                        }
                    }
                    else
                    {
                        // Push the tag on to the stack
                        tags.Push(tag);
                    }
                }
            }
            return Tuple.Create(seenClose, blockAlreadyBuilt);
        }

        private Tuple<bool, bool> ParseEndTag(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            in SyntaxListBuilder<RazorSyntaxNode> parentBuilder,
            SourceLocation tagStart,
            Stack<Tuple<SyntaxToken, SourceLocation>> tags)
        {
            // Accept "/" and move next
            Assert(SyntaxKind.ForwardSlash);
            var forwardSlash = CurrentToken;
            if (!NextToken())
            {
                Accept(_bufferedOpenAngle);
                Accept(forwardSlash);
                return Tuple.Create(false, false);
            }
            else
            {
                var tagName = string.Empty;
                SyntaxToken bangToken = null;

                if (At(SyntaxKind.Bang))
                {
                    bangToken = CurrentToken;

                    var nextToken = Lookahead(count: 1);

                    if (nextToken != null && nextToken.Kind == SyntaxKind.Text)
                    {
                        tagName = "!" + nextToken.Content;
                    }
                }
                else if (At(SyntaxKind.Text))
                {
                    tagName = CurrentToken.Content;
                }

                var matched = RemoveTag(tags, tagName, tagStart);

                if (tags.Count == 0 &&
                    // Note tagName may contain a '!' escape character. This ensures </!text> doesn't match here.
                    // </!text> tags are treated like any other escaped HTML end tag.
                    string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) &&
                    matched)
                {
                    return EndTextTag(builder, parentBuilder, forwardSlash);
                }
                Accept(_bufferedOpenAngle);
                Accept(forwardSlash);

                ParseOptionalBangEscape(builder);

                AcceptUntil(SyntaxKind.CloseAngle);

                // Accept the ">"
                return Tuple.Create(TryAccept(SyntaxKind.CloseAngle), false);
            }
        }

        private Tuple<bool, bool> EndTextTag(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            in SyntaxListBuilder<RazorSyntaxNode> parentBuilder,
            SyntaxToken solidus)
        {
            Accept(_bufferedOpenAngle);
            Accept(solidus);

            var textLocation = CurrentStart;
            Assert(SyntaxKind.Text);
            AcceptAndMoveNext();

            var seenCloseAngle = TryAccept(SyntaxKind.CloseAngle);

            if (!seenCloseAngle)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                        new SourceSpan(textLocation, contentLength: 4 /* text */)));

                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                RecoverTextTag();
            }
            else
            {
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }

            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;

            var transition = GetNodeWithSpanContext(SyntaxFactory.MarkupTransition(Output()));
            builder.Add(transition);
            var tagBlock = SyntaxFactory.MarkupTagBlock(builder.ToList());
            parentBuilder.Add(tagBlock);

            return Tuple.Create(seenCloseAngle, true);
        }

        private bool RemoveTag(Stack<Tuple<SyntaxToken, SourceLocation>> tags, string tagName, SourceLocation tagStart)
        {
            Tuple<SyntaxToken, SourceLocation> currentTag = null;
            while (tags.Count > 0)
            {
                currentTag = tags.Pop();
                if (string.Equals(tagName, currentTag.Item1.Content, StringComparison.OrdinalIgnoreCase))
                {
                    // Matched the tag
                    return true;
                }
            }
            if (currentTag != null)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                        new SourceSpan(
                            SourceLocationTracker.Advance(currentTag.Item2, "<"),
                            currentTag.Item1.Content.Length),
                        currentTag.Item1.Content));
            }
            else
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                        new SourceSpan(SourceLocationTracker.Advance(tagStart, "</"), tagName.Length), tagName));
            }
            return false;
        }

        private void RecoverTextTag()
        {
            // We don't want to skip-to and parse because there shouldn't be anything in the body of text tags.
            AcceptUntil(SyntaxKind.CloseAngle, SyntaxKind.NewLine);

            // Include the close angle in the text tag block if it's there, otherwise just move on
            TryAccept(SyntaxKind.CloseAngle);
        }

        private void EndTagBlock(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            Stack<Tuple<SyntaxToken, SourceLocation>> tags,
            bool complete)
        {
            if (tags.Count > 0)
            {
                // Ended because of EOF, not matching close tag.  Throw error for last tag
                while (tags.Count > 1)
                {
                    tags.Pop();
                }
                var tag = tags.Pop();
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                        new SourceSpan(
                            SourceLocationTracker.Advance(tag.Item2, "<"),
                            tag.Item1.Content.Length),
                        tag.Item1.Content));
            }
            else if (complete)
            {
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }
            tags.Clear();
            if (!Context.DesignTimeMode)
            {
                var shouldAcceptWhitespaceAndNewLine = true;

                // Check if the previous span was a transition.
                var previousSpan = builder.Count > 0 ? GetLastSpan(builder[builder.Count - 1]) : null;
                if (previousSpan != null && previousSpan.Kind == SyntaxKind.MarkupTransition)
                {
                    var tokens = ReadWhile(
                        f => (f.Kind == SyntaxKind.Whitespace) || (f.Kind == SyntaxKind.NewLine));

                    // Make sure the current token is not markup, which can be html start tag or @:
                    if (!(At(SyntaxKind.OpenAngle) ||
                        (At(SyntaxKind.Transition) && Lookahead(count: 1).Content.StartsWith(":"))))
                    {
                        // Don't accept whitespace as markup if the end text tag is followed by csharp.
                        shouldAcceptWhitespaceAndNewLine = false;
                    }

                    PutCurrentBack();
                    PutBack(tokens);
                    EnsureCurrent();
                }

                if (shouldAcceptWhitespaceAndNewLine)
                {
                    // Accept whitespace and a single newline if present
                    AcceptWhile(SyntaxKind.Whitespace);
                    TryAccept(SyntaxKind.NewLine);
                }
            }
            else if (SpanContext.EditHandler.AcceptedCharacters == AcceptedCharactersInternal.Any)
            {
                AcceptWhile(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.NewLine);
            }
            PutCurrentBack();

            if (!complete)
            {
                AcceptMarkerTokenIfNecessary();
            }

            builder.Add(OutputAsMarkupLiteral());
        }

        public MarkupBlockSyntax ParseRazorBlock(Tuple<string, string> nestingSequences, bool caseSensitive)
        {
            if (Context == null)
            {
                throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
            }

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            using (PushSpanContextConfig(DefaultMarkupSpanContext))
            {
                var builder = pooledResult.Builder;

                NextToken();
                CaseSensitive = caseSensitive;
                NestingBlock(builder, nestingSequences);
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputAsMarkupLiteral());

                return SyntaxFactory.MarkupBlock(builder.ToList());
            }
        }

        private void NestingBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Tuple<string, string> nestingSequences)
        {
            var nesting = 1;
            while (nesting > 0 && !EndOfFile)
            {
                SkipToAndParseCode(builder, token =>
                    token.Kind == SyntaxKind.Text ||
                    token.Kind == SyntaxKind.OpenAngle);
                if (At(SyntaxKind.Text))
                {
                    nesting += ProcessTextToken(builder, nestingSequences, nesting);
                    if (CurrentToken != null)
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
                    ParseTagInDocumentContext(builder);
                }
            }
        }

        private int ProcessTextToken(in SyntaxListBuilder<RazorSyntaxNode> builder, Tuple<string, string> nestingSequences, int currentNesting)
        {
            for (var i = 0; i < CurrentToken.Content.Length; i++)
            {
                var nestingDelta = HandleNestingSequence(builder, nestingSequences.Item1, i, currentNesting, 1);
                if (nestingDelta == 0)
                {
                    nestingDelta = HandleNestingSequence(builder, nestingSequences.Item2, i, currentNesting, -1);
                }

                if (nestingDelta != 0)
                {
                    return nestingDelta;
                }
            }
            return 0;
        }

        private int HandleNestingSequence(in SyntaxListBuilder<RazorSyntaxNode> builder, string sequence, int position, int currentNesting, int retIfMatched)
        {
            if (sequence != null &&
                CurrentToken.Content[position] == sequence[0] &&
                position + sequence.Length <= CurrentToken.Content.Length)
            {
                var possibleStart = CurrentToken.Content.Substring(position, sequence.Length);
                if (string.Equals(possibleStart, sequence, Comparison))
                {
                    // Capture the current token and "put it back" (really we just want to clear CurrentToken)
                    var bookmark = CurrentStart;
                    var token = CurrentToken;
                    PutCurrentBack();

                    // Carve up the token
                    var pair = Language.SplitToken(token, position, SyntaxKind.Text);
                    var preSequence = pair.Item1;
                    Debug.Assert(pair.Item2 != null);
                    pair = Language.SplitToken(pair.Item2, sequence.Length, SyntaxKind.Text);
                    var sequenceToken = pair.Item1;
                    var postSequence = pair.Item2;
                    var postSequenceBookmark = bookmark.AbsoluteIndex + preSequence.Content.Length + pair.Item1.Content.Length;

                    // Accept the first chunk (up to the nesting sequence we just saw)
                    if (!string.IsNullOrEmpty(preSequence.Content))
                    {
                        Accept(preSequence);
                    }

                    if (currentNesting + retIfMatched == 0)
                    {
                        // This is 'popping' the final entry on the stack of nesting sequences
                        // A caller higher in the parsing stack will accept the sequence token, so advance
                        // to it
                        Context.Source.Position = bookmark.AbsoluteIndex + preSequence.Content.Length;
                    }
                    else
                    {
                        // This isn't the end of the last nesting sequence, accept the token and keep going
                        Accept(sequenceToken);

                        // Position at the start of the postSequence token, which might be null.
                        Context.Source.Position = postSequenceBookmark;
                    }

                    // Return the value we were asked to return if matched, since we found a nesting sequence
                    return retIfMatched;
                }
            }
            return 0;
        }

        private Syntax.GreenNode GetLastSpan(RazorSyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            // Find the last token of this node and return its immediate non-list parent.
            var red = node.CreateRed();
            var last = red.GetLastTerminal();
            if (last == null)
            {
                return null;
            }

            while (last.Green.IsToken || last.Green.IsList)
            {
                last = last.Parent;
            }

            return last.Green;
        }

        private void DefaultMarkupSpanContext(SpanContextBuilder spanContext)
        {
            spanContext.ChunkGenerator = new MarkupChunkGenerator();
            spanContext.EditHandler = new SpanEditHandler(Language.TokenizeString, AcceptedCharactersInternal.Any);
        }

        private void OtherParserBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            AcceptMarkerTokenIfNecessary();
            builder.Add(OutputAsMarkupLiteral());

            RazorSyntaxNode codeBlock;
            using (PushSpanContextConfig())
            {
                codeBlock = CodeParser.ParseBlock();
            }

            builder.Add(codeBlock);
            InitializeContext(SpanContext);
            NextToken();
        }

        protected static Func<SyntaxToken, bool> IsSpacingToken(bool includeNewLines)
        {
            return token => token.Kind == SyntaxKind.Whitespace || (includeNewLines && token.Kind == SyntaxKind.NewLine);
        }

        internal static bool IsValidAttributeNameToken(SyntaxToken token)
        {
            if (token == null)
            {
                return false;
            }

            // These restrictions cover most of the spec defined: http://www.w3.org/TR/html5/syntax.html#attributes-0
            // However, it's not all of it. For instance we don't special case control characters or allow OpenAngle.
            // It also doesn't try to exclude Razor specific features such as the @ transition. This is based on the
            // expectation that the parser handles such scenarios prior to falling through to name resolution.
            var tokenType = token.Kind;
            return tokenType != SyntaxKind.Whitespace &&
                tokenType != SyntaxKind.NewLine &&
                tokenType != SyntaxKind.CloseAngle &&
                tokenType != SyntaxKind.OpenAngle &&
                tokenType != SyntaxKind.ForwardSlash &&
                tokenType != SyntaxKind.DoubleQuote &&
                tokenType != SyntaxKind.SingleQuote &&
                tokenType != SyntaxKind.Equals &&
                tokenType != SyntaxKind.Marker;
        }
    }
}
