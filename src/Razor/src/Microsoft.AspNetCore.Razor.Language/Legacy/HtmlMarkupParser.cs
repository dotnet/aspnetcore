// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal partial class HtmlMarkupParser : TokenizerBackedParser<HtmlTokenizer>
    {
        private const string ScriptTagName = "script";

        private static readonly char[] ValidAfterTypeAttributeNameCharacters = { ' ', '\t', '\r', '\n', '\f', '=' };
        private static readonly SyntaxToken[] nonAllowedHtmlCommentEnding = new[]
        {
            SyntaxFactory.Token(SyntaxKind.Text, "-"),
            SyntaxFactory.Token(SyntaxKind.Bang, "!"),
            SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"),
        };

        public HtmlMarkupParser(ParserContext context)
            : base(context.ParseLeadingDirectives ? FirstDirectiveHtmlLanguageCharacteristics.Instance : HtmlLanguageCharacteristics.Instance, context)
        {
        }

        public CSharpCodeParser CodeParser { get; set; }

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

                ParseMarkupNodes(builder, ParseMode.Markup);
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputAsMarkupLiteral());

                var markup = SyntaxFactory.MarkupBlock(builder.ToList());

                return SyntaxFactory.RazorDocument(markup);
            }
        }

        private void ParseMarkupNodes(
            in SyntaxListBuilder<RazorSyntaxNode> builder,
            ParseMode mode,
            Func<SyntaxToken, bool> stopCondition = null)
        {
            stopCondition = stopCondition ?? (token => false);
            while (!EndOfFile && !stopCondition(CurrentToken))
            {
                ParseMarkupNode(builder, mode);
            }
        }

        private void ParseMarkupNode(in SyntaxListBuilder<RazorSyntaxNode> builder, ParseMode mode)
        {
            switch (GetParserState(mode))
            {
                case ParserState.MarkupText:
                    ParseMarkupText(builder);
                    break;
                case ParserState.Tag:
                    ParseMarkupElement(builder);
                    break;
                case ParserState.SpecialTag:
                    ParseSpecialTag(builder);
                    break;
                case ParserState.XmlPI:
                    ParseXmlPI(builder);
                    break;
                case ParserState.CData:
                    ParseCData(builder);
                    break;
                case ParserState.MarkupComment:
                    ParseMarkupComment(builder);
                    break;
                case ParserState.RazorComment:
                    ParseRazorCommentWithLeadingAndTrailingWhitespace(builder);
                    break;
                case ParserState.DoubleTransition:
                    ParseDoubleTransition(builder);
                    break;
                case ParserState.CodeTransition:
                    ParseCodeTransition(builder);
                    break;
                case ParserState.Misc:
                    ParseMisc(builder);
                    break;
                case ParserState.Unknown:
                    AcceptAndMoveNext();
                    break;
                case ParserState.EOF:
                    builder.Add(OutputAsMarkupLiteral());
                    break;
            }
        }

        private void ParseMarkupText(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            AcceptAndMoveNext();
        }

        private void ParseMarkupElement(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            Assert(SyntaxKind.OpenAngle);

            // Output already accepted tokens if any.
            builder.Add(OutputAsMarkupLiteral());

            if (!NextIs(SyntaxKind.ForwardSlash))
            {
                // Parsing a start tag
                var startTag = ParseStartTag(out var tagName, out var tagMode);
                builder.Add(startTag);

                if (tagMode == MarkupTagMode.Script)
                {
                    ParseJavascriptAndEndScriptTag(builder);
                    return;
                }
                else if (tagMode != MarkupTagMode.Void && tagMode != MarkupTagMode.SelfClosing)
                {
                    var body = ParseTagBody(tagName, out var seenEndTag);
                    if (body != null)
                    {
                        builder.AddRange(body);
                    }

                    if (seenEndTag)
                    {
                        var endTag = ParseEndTag();
                        builder.Add(endTag);
                    }
                }
            }
            else
            {
                // Parsing an end tag.
                var endTag = ParseEndTag();
                builder.Add(endTag);
            }
        }

        private MarkupTagBlockSyntax ParseStartTag(out string tagName, out MarkupTagMode tagMode)
        {
            tagName = null;
            tagMode = MarkupTagMode.Normal;
            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var tagBuilder = pooledResult.Builder;

                AcceptAndMoveNext(); // Accept '<'
                TryParseBangEscape(tagBuilder);

                if (At(SyntaxKind.Text))
                {
                    tagName = CurrentToken.Content;
                    if (ParserHelpers.VoidElements.Contains(tagName))
                    {
                        // This is a void element.
                        tagMode = MarkupTagMode.Void;
                    }
                }
                TryAccept(SyntaxKind.Text);

                // Parse the contents of a tag like attributes.
                ParseTagContent(tagBuilder);

                if (TryAccept(SyntaxKind.ForwardSlash))
                {
                    // This is a self closing tag.
                    tagMode = MarkupTagMode.SelfClosing;
                }
                TryAccept(SyntaxKind.CloseAngle);

                // End tag block
                tagBuilder.Add(OutputAsMarkupLiteral());
                var tagBlock = SyntaxFactory.MarkupTagBlock(tagBuilder.ToList());
                if (string.Equals(tagName, ScriptTagName, StringComparison.OrdinalIgnoreCase))
                {
                    // If the script tag expects javascript content then we should do minimal parsing until we reach
                    // the end script tag. Don't want to incorrectly parse a "var tag = '<input />';" as an HTML tag.
                    if (!ScriptTagExpectsHtml(tagBlock))
                    {
                        tagMode = MarkupTagMode.Script;
                    }
                }

                return tagBlock;
            }
        }

        private MarkupTagBlockSyntax ParseEndTag()
        {
            // This section can accept things like: '</p  >' or '</p>' etc.
            Assert(SyntaxKind.OpenAngle);

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var tagBuilder = pooledResult.Builder;

                AcceptAndMoveNext(); // Accept '<'
                TryAccept(SyntaxKind.ForwardSlash);

                // Whitespace here is invalid (according to the spec)
                TryParseBangEscape(tagBuilder);
                TryAccept(SyntaxKind.Text);
                TryAccept(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.CloseAngle);

                // End tag block
                tagBuilder.Add(OutputAsMarkupLiteral());
                var tagBlock = SyntaxFactory.MarkupTagBlock(tagBuilder.ToList());
                return tagBlock;
            }
        }

        private SyntaxList<RazorSyntaxNode> ParseTagBody(string tagName, out bool seenEndTag)
        {
            // No-op here for now.
            seenEndTag = false;
            return null;
        }

        private void ParseTagContent(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Calling to legacy code for now.
            LegacyParseTagContent(builder);
        }

        private void ParseJavascriptAndEndScriptTag(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal endTagAcceptedCharacters = AcceptedCharactersInternal.Any)
        {
            // Special case for <script>: Skip to end of script tag and parse code
            var seenEndScript = false;

            while (!seenEndScript && !EndOfFile)
            {
                ParseMarkupNodes(builder, ParseMode.Text, token => token.Kind == SyntaxKind.OpenAngle);
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

                if (!seenEndScript)
                {
                    AcceptAndMoveNext(); // Accept '<' (not the closing script tag's open angle)
                }
            }

            if (seenEndScript)
            {
                var tagStart = CurrentStart;
                builder.Add(OutputAsMarkupLiteral());

                using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
                {
                    var tagBuilder = pooledResult.Builder;
                    SpanContext.EditHandler.AcceptedCharacters = endTagAcceptedCharacters;

                    AcceptAndMoveNext(); // '<'
                    AcceptAndMoveNext(); // '/'
                    ParseMarkupNodes(tagBuilder, ParseMode.Text, token => token.Kind == SyntaxKind.CloseAngle);
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
        }

        private bool ParseSpecialTag(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // Clear the current token builder.
            builder.Add(OutputAsMarkupLiteral());

            return AcceptTokenUntilAll(builder, SyntaxKind.CloseAngle);
        }

        private bool ParseXmlPI(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            Assert(SyntaxKind.OpenAngle);
            AcceptAndMoveNext();
            Assert(SyntaxKind.QuestionMark);
            AcceptAndMoveNext();
            return AcceptTokenUntilAll(builder, SyntaxKind.QuestionMark, SyntaxKind.CloseAngle);
        }

        private bool ParseCData(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            // <![CDATA[...]]>
            Assert(SyntaxKind.OpenAngle);
            AcceptAndMoveNext(); // '<'
            AcceptAndMoveNext(); // '!'
            AcceptAndMoveNext(); // '['
            Debug.Assert(CurrentToken.Kind == SyntaxKind.Text && string.Equals(CurrentToken.Content, "cdata", StringComparison.OrdinalIgnoreCase));
            AcceptAndMoveNext();
            Assert(SyntaxKind.LeftBracket);
            return AcceptTokenUntilAll(builder, SyntaxKind.RightBracket, SyntaxKind.RightBracket, SyntaxKind.CloseAngle);
        }

        private void ParseDoubleTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            AcceptWhile(IsSpacingToken(includeNewLines: true));
            builder.Add(OutputAsMarkupLiteral());

            // First transition
            Assert(SyntaxKind.Transition);
            AcceptAndMoveNext();
            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            builder.Add(OutputAsMarkupEphemeralLiteral());

            // Second transition
            AcceptAndMoveNext();
        }

        private void ParseCodeTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (Context.NullGenerateWhitespaceAndNewLine)
            {
                // Usually this is set to true when a Code block ends and there is whitespace left after it.
                // We don't want to write it to output.
                Context.NullGenerateWhitespaceAndNewLine = false;
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                AcceptWhile(IsSpacingToken(includeNewLines: false));
                if (At(SyntaxKind.NewLine))
                {
                    AcceptAndMoveNext();
                }

                builder.Add(OutputAsMarkupEphemeralLiteral());
            }

            var lastWhitespace = AcceptWhitespaceInLines();
            if (lastWhitespace != null)
            {
                if (Context.DesignTimeMode || !Context.StartOfLine)
                {
                    // Markup owns whitespace in design time mode.
                    Accept(lastWhitespace);
                    lastWhitespace = null;
                }
            }

            PutCurrentBack();
            if (lastWhitespace != null)
            {
                PutBack(lastWhitespace);
            }

            OtherParserBlock(builder);
        }

        private void ParseMarkupComment(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            Assert(SyntaxKind.OpenAngle);

            // Clear the current token builder.
            builder.Add(OutputAsMarkupLiteral());

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var htmlCommentBuilder = pooledResult.Builder;

                // Accept the '<', '!' and double-hyphen token at the beginning of the comment block.
                AcceptAndMoveNext();
                AcceptAndMoveNext();
                AcceptAndMoveNext();
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                htmlCommentBuilder.Add(OutputAsMarkupLiteral());

                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;
                while (!EndOfFile)
                {
                    ParseMarkupNodes(htmlCommentBuilder, ParseMode.Text, t => t.Kind == SyntaxKind.DoubleHyphen);
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
                        return;
                    }
                    else if (lastDoubleHyphen != null)
                    {
                        Accept(lastDoubleHyphen);
                    }
                }

                builder.Add(OutputAsMarkupLiteral());
            }
        }

        private void ParseRazorCommentWithLeadingAndTrailingWhitespace(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            var shouldRenderWhitespace = true;
            var lastWhitespace = AcceptWhitespaceInLines();
            var startOfLine = Context.StartOfLine;
            if (lastWhitespace != null)
            {
                // Don't render the whitespace between the start of the line and the razor comment.
                if (startOfLine)
                {
                    AcceptMarkerTokenIfNecessary();
                    // Output the tokens that may have been accepted prior to the whitespace.
                    builder.Add(OutputAsMarkupLiteral());

                    SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                    shouldRenderWhitespace = false;
                }

                Accept(lastWhitespace);
                lastWhitespace = null;
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

        private void ParseMisc(in SyntaxListBuilder<RazorSyntaxNode> builder)
        {
            if (Context.NullGenerateWhitespaceAndNewLine)
            {
                // Usually this is set to true when a Code block ends and there is whitespace left after it.
                // We don't want to write it to output.
                Context.NullGenerateWhitespaceAndNewLine = false;
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                AcceptWhile(IsSpacingToken(includeNewLines: false));
                if (At(SyntaxKind.NewLine))
                {
                    AcceptAndMoveNext();
                }

                builder.Add(OutputAsMarkupEphemeralLiteral());
            }

            AcceptWhile(IsSpacingToken(includeNewLines: true));
        }

        private bool ScriptTagExpectsHtml(MarkupTagBlockSyntax tagBlock)
        {
            MarkupAttributeBlockSyntax typeAttribute = null;
            for (var i = 0; i < tagBlock.Children.Count; i++)
            {
                var node = tagBlock.Children[i];
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

        // Internal for testing.
        internal SyntaxToken AcceptAllButLastDoubleHyphens()
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
                ParseMarkupNodes(builder, ParseMode.Text, t => t.Kind == endSequence[0]);
                if (AcceptAll(endSequence))
                {
                    return true;
                }
            }
            Debug.Assert(EndOfFile);
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
            return false;
        }

        private ParserState GetParserState(ParseMode mode)
        {
            var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true));
            try
            {
                if (!whitespace.Any() && EndOfFile)
                {
                    return ParserState.EOF;
                }
                else if (At(SyntaxKind.RazorCommentTransition))
                {
                    // Let the comment parser handle the preceding whitespace.
                    return ParserState.RazorComment;
                }
                else if (At(SyntaxKind.Transition))
                {
                    if (NextIs(SyntaxKind.Transition))
                    {
                        return ParserState.DoubleTransition;
                    }

                    // Let the transition parser handle the preceding whitespace.
                    return ParserState.CodeTransition;
                }
                else if (whitespace.Any())
                {
                    // This whitespace isn't sensitive to what comes after it.
                    return ParserState.Misc;
                }
                else if (mode == ParseMode.Text)
                {
                    // We don't want to parse as tags in Text mode. We do this for cases like script tags or <!-- -->.
                    return ParserState.MarkupText;
                }
                else if (At(SyntaxKind.OpenAngle))
                {
                    if (NextIs(SyntaxKind.Bang))
                    {
                        // Checking to see if we meet the conditions of a special '!' tag: <!DOCTYPE, <![CDATA[, <!--.
                        if (!IsBangEscape(lookahead: 1))
                        {
                            if (IsHtmlCommentAhead())
                            {
                                return ParserState.MarkupComment;
                            }
                            else if (Lookahead(2)?.Kind == SyntaxKind.LeftBracket &&
                                Lookahead(3) is SyntaxToken tagName &&
                                string.Equals(tagName.Content, "cdata", StringComparison.OrdinalIgnoreCase) &&
                                Lookahead(4)?.Kind == SyntaxKind.LeftBracket)
                            {
                                return ParserState.CData;
                            }
                            else
                            {
                                // E.g. <!DOCTYPE ...
                                return ParserState.SpecialTag;
                            }
                        }
                    }
                    else if (NextIs(SyntaxKind.QuestionMark))
                    {
                        return ParserState.XmlPI;
                    }

                    // Regular tag
                    return ParserState.Tag;
                }
                else
                {
                    return ParserState.Unknown;
                }
            }
            finally
            {
                if (whitespace.Any())
                {
                    PutCurrentBack();
                    PutBack(whitespace);
                    EnsureCurrent();
                }
            }
        }

        private bool TryParseBangEscape(in SyntaxListBuilder<RazorSyntaxNode> builder)
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
                return true;
            }

            return false;
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

        private bool IsHtmlCommentAhead()
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

            if (!(At(SyntaxKind.OpenAngle) && NextIs(SyntaxKind.Bang)))
            {
                return false;
            }

            // Consume '<' and '!'
            var openAngle = EatCurrentToken();
            var bangToken = EatCurrentToken();

            try
            {
                if (EndOfFile || CurrentToken.Kind != SyntaxKind.DoubleHyphen)
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
            finally
            {
                // Put back the consumed tokens for later parsing.
                PutCurrentBack();
                PutBack(bangToken);
                PutBack(openAngle);
                EnsureCurrent();
            }
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

        protected static Func<SyntaxToken, bool> IsSpacingToken(bool includeNewLines)
        {
            return token => token.Kind == SyntaxKind.Whitespace || (includeNewLines && token.Kind == SyntaxKind.NewLine);
        }

        internal static bool IsHyphen(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.Text && token.Content == "-";
        }

        private void DefaultMarkupSpanContext(SpanContextBuilder spanContext)
        {
            spanContext.ChunkGenerator = new MarkupChunkGenerator();
            spanContext.EditHandler = new SpanEditHandler(Language.TokenizeString, AcceptedCharactersInternal.Any);
        }

        private enum ParseMode
        {
            Markup,
            Text,
        }

        private enum MarkupTagMode
        {
            Normal,
            Void,
            SelfClosing,
            Script
        }
    }
}
