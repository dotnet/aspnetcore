// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class HtmlMarkupParser : TokenizerBackedParser<HtmlTokenizer>
{
    private const string ScriptTagName = "script";
    private static readonly SyntaxList<RazorSyntaxNode> EmptySyntaxList = new SyntaxListBuilder<RazorSyntaxNode>(0).ToList();

    private static readonly char[] ValidAfterTypeAttributeNameCharacters = { ' ', '\t', '\r', '\n', '\f', '=' };
    private static readonly SyntaxToken[] nonAllowedHtmlCommentEnding = new[]
    {
            SyntaxFactory.Token(SyntaxKind.Text, "-"),
            SyntaxFactory.Token(SyntaxKind.Bang, "!"),
            SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"),
        };

    private Stack<TagTracker> _tagTracker = new Stack<TagTracker>();

    public HtmlMarkupParser(ParserContext context)
        : base(context.ParseLeadingDirectives ? FirstDirectiveHtmlLanguageCharacteristics.Instance : HtmlLanguageCharacteristics.Instance, context)
    {
    }

    private TagTracker CurrentTracker => _tagTracker.Count > 0 ? _tagTracker.Peek() : null;

    private string CurrentStartTagName => CurrentTracker?.TagName;

    public CSharpCodeParser CodeParser { get; set; }

    private bool CaseSensitive { get; set; }

    private StringComparison Comparison
    {
        get { return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
    }

    //
    // This is the main entry point into the Razor parser. This will be called only once.
    // Anything outside of code blocks like @{} are parsed here.
    // This calls into the code parser whenever a '@' transition is encountered.
    // In this mode, we group markup elements with the appropriate Start tag, End tag and body
    // but we don't perform any validation on the structure. We don't produce errors for cases like missing end tags etc.
    // We let the editor take care of that.
    //
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

            // If we are still tracking any unclosed start tags, we need to close them.
            while (_tagTracker.Count > 0)
            {
                var tracker = _tagTracker.Pop();
                if (IsVoidElement(tracker.TagName))
                {
                    // We were tracking a void element but we reached the end of the document without finding a matching end tag.
                    // So, close that element and move its content to its parent.
                    var children = builder.Consume();
                    var voidElement = SyntaxFactory.MarkupElement(tracker.StartTag, EmptySyntaxList, endTag: null);
                    builder.AddRange(tracker.PreviousNodes);
                    builder.Add(voidElement);
                    builder.AddRange(children);
                }
                else
                {
                    var element = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag: null);
                    builder.AddRange(tracker.PreviousNodes);
                    builder.Add(element);
                }
            }

            var markup = SyntaxFactory.MarkupBlock(builder.ToList());

            return SyntaxFactory.RazorDocument(markup);
        }
    }

    //
    // This will be called by the code parser whenever any markup is encountered inside a code block @{}.
    // It can either be a single line markup like @: or a tag. In this case, we want to keep track of tag nesting
    // and add appropriate errors for any malformed cases.
    // In addition to parsing regular tags, we also understand special "text" tags. These tags are not rendered to the output
    // but used to render a block of text it encloses as markup. They are a multiline alternative to the single line markup syntax @:
    // One caveat is that the tags in single markup as parsed as plain text.
    //
    // The tag stack inside a code block is different from the stack outside the block.
    // E.g, `<div> @{ </div> }` will be parsed as two separate elements with missing end and start tags respectively.
    //
    public MarkupBlockSyntax ParseBlock()
    {
        if (Context == null)
        {
            throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
        }

        var oldTagTracker = _tagTracker;
        try
        {
            // This is the start of a new block. We don't want the current tag stack to mix with the tags in this block.
            // Initialize a new stack.
            _tagTracker = new Stack<TagTracker>();
            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            using (PushSpanContextConfig(DefaultMarkupSpanContext))
            {
                var builder = pooledResult.Builder;
                if (!NextToken())
                {
                    return null;
                }

                AcceptWhile(IsSpacingTokenIncludingNewLines);
                builder.Add(OutputAsMarkupLiteral());

                if (At(SyntaxKind.OpenAngle))
                {
                    ParseMarkupInCodeBlock(builder);
                }
                else if (At(SyntaxKind.Transition))
                {
                    ParseMarkupTransition(builder);
                }
                else
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_MarkupBlockMustStartWithTag(
                            new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                }

                // Add any remaining tokens to the builder.
                builder.Add(OutputAsMarkupLiteral());

                var markupBlock = builder.ToList();

                return SyntaxFactory.MarkupBlock(markupBlock);
            }
        }
        finally
        {
            _tagTracker = oldTagTracker;
        }
    }

    //
    // This is called when the body of a Razor block directive needs to be parsed. E.g @section |{ ... }|
    // This parses markup in 'document' mode, which means we don't add any errors for malformed tags.
    // Since, a razor block can also have several code blocks @{} within it, we need to keep track of the block nesting level
    // to make sure we exit when we reach the final '}'.
    //
    // Similar to ParseBlock, the tag stack inside a razor block is different from the stack outside the block.
    // E.g, `@section Foo { </div> } <div>` will be parsed as two separate elements.
    //
    public MarkupBlockSyntax ParseRazorBlock(Tuple<string, string> nestingSequences, bool caseSensitive)
    {
        if (Context == null)
        {
            throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
        }

        var oldTagTracker = _tagTracker;
        try
        {
            // This is the start of a new block. We don't want the current tag stack to mix with the tags in this block.
            // Initialize a new stack.
            _tagTracker = new Stack<TagTracker>();
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
        finally
        {
            _tagTracker = oldTagTracker;
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
                ParseMarkupElement(builder, mode);
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

    private void ParseMarkupInCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        do
        {
            switch (GetParserState(ParseMode.MarkupInCodeBlock))
            {
                case ParserState.EOF:
                    break;
                case ParserState.Tag:
                    ParseMarkupElement(builder, ParseMode.MarkupInCodeBlock);
                    break;
                case ParserState.SpecialTag:
                case ParserState.XmlPI:
                case ParserState.MarkupComment:
                case ParserState.CData:
                    ParseMarkupNode(builder, ParseMode.MarkupInCodeBlock);
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                    builder.Add(OutputAsMarkupLiteral());
                    break;
                default:
                    ParseMarkupNode(builder, ParseMode.Text);
                    break;
            }
        } while (!EndOfFile && _tagTracker.Count > 0);

        CompleteMarkupInCodeBlock(builder);
    }

    private void CompleteMarkupInCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Output anything we've accepted so far.
        builder.Add(OutputAsMarkupLiteral());

        var isOuterTagWellFormed = true;
        while (_tagTracker.Count > 0)
        {
            var tracker = _tagTracker.Pop();
            var element = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag: null);
            builder.AddRange(tracker.PreviousNodes);
            builder.Add(element);

            if (_tagTracker.Count == 0)
            {
                isOuterTagWellFormed = tracker.IsWellFormed;
                if (isOuterTagWellFormed)
                {
                    // We're at the outermost start tag. Add an error.
                    // We don't want to add this error if the tag is unfinished. A different error would have already been added.
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                            new SourceSpan(
                                SourceLocationTracker.Advance(tracker.TagLocation, "<"),
                                tracker.TagName.Length),
                            tracker.TagName));
                }
            }
        }

        if (!Context.DesignTimeMode)
        {
            // We want to accept the whitespace and newline at the end of the markup.
            // E.g,
            // @{
            //     <div>Foo</div>|
            // |}
            // Except in two cases,
            // 1. Design time
            // 2. Text tags
            //
            var shouldAcceptWhitespaceAndNewLine = true;

            // Check if the previous span was a transition.
            var previousSpan = builder.Count > 0 ? GetLastSpan(builder[builder.Count - 1]) : null;
            if (previousSpan != null &&
                ((previousSpan is MarkupStartTagSyntax startTag && startTag.IsMarkupTransition) ||
                (previousSpan is MarkupEndTagSyntax endTag && endTag.IsMarkupTransition)))
            {
                var tokens = ReadWhile(
                    static f => (f.Kind == SyntaxKind.Whitespace) || (f.Kind == SyntaxKind.NewLine));

                // Make sure the current token is not markup, which can be html start tag or @:
                if (!(At(SyntaxKind.OpenAngle) ||
                    (At(SyntaxKind.Transition) && Lookahead(count: 1).Content.StartsWith(":", StringComparison.Ordinal))))
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

                if (isOuterTagWellFormed)
                {
                    // Completed tags have no accepted characters inside blocks.
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                }
            }
        }

        PutCurrentBack();
        if (!isOuterTagWellFormed)
        {
            AcceptMarkerTokenIfNecessary();
        }

        builder.Add(OutputAsMarkupLiteral());
    }

    private void ParseMarkupTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(SyntaxKind.Transition);

        AcceptAndMoveNext();
        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
        var transition = GetNodeWithSpanContext(SyntaxFactory.MarkupTransition(Output()));
        builder.Add(transition);

        // "@:" => Explicit Single Line Block
        if (CurrentToken.Kind == SyntaxKind.Text && CurrentToken.Content.Length > 0 && CurrentToken.Content[0] == ':')
        {
            // Split the token
            var split = Language.SplitToken(CurrentToken, 1, SyntaxKind.Colon);

            // The first part (left) is output as MetaCode
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
            // Template
            // E.g, @<div>Foo</div>
            ParseMarkupInCodeBlock(builder);
        }
    }

    private void ParseSingleLineMarkup(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Parse until a newline.
        // First, signal to code parser that whitespace is significant to us.
        var old = Context.WhiteSpaceIsSignificantToAncestorBlock;
        Context.WhiteSpaceIsSignificantToAncestorBlock = true;
        SpanContext.EditHandler = new SpanEditHandler(LanguageTokenizeString);

        // Now parse until a new line.
        do
        {
            ParseMarkupNodes(builder, ParseMode.Text, token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);
            if (At(SyntaxKind.Whitespace))
            {
                AcceptAndMoveNext();
            }
        } while (!EndOfFile && CurrentToken.Kind != SyntaxKind.NewLine);

        if (!EndOfFile && CurrentToken.Kind == SyntaxKind.NewLine)
        {
            AcceptAndMoveNext();
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
        }
        PutCurrentBack();
        Context.WhiteSpaceIsSignificantToAncestorBlock = old;
        builder.Add(OutputAsMarkupLiteral());
    }

    private void ParseMarkupElement(in SyntaxListBuilder<RazorSyntaxNode> builder, ParseMode mode)
    {
        Assert(SyntaxKind.OpenAngle);

        // Output already accepted tokens if any.
        builder.Add(OutputAsMarkupLiteral());

        if (!NextIs(SyntaxKind.ForwardSlash))
        {
            // Parsing a start tag
            var tagStart = CurrentStart;
            var startTag = ParseStartTag(mode, tagStart, out var tagName, out var tagMode, out var isWellFormed);
            if (tagMode == MarkupTagMode.Script)
            {
                var acceptedCharacters = mode == ParseMode.MarkupInCodeBlock ? AcceptedCharactersInternal.None : AcceptedCharactersInternal.Any;
                ParseJavascriptAndEndScriptTag(builder, startTag, acceptedCharacters);
                return;
            }

            if (tagMode == MarkupTagMode.SelfClosing || tagMode == MarkupTagMode.Invalid || tagMode == MarkupTagMode.Void)
            {
                // For cases like <foo />, <input> or invalid cases like |<|<p>
                var element = SyntaxFactory.MarkupElement(startTag, EmptySyntaxList, endTag: null);
                builder.Add(element);
                return;
            }
            else
            {
                // This is a normal start tag. We need to keep track of it.
                var tracker = new TagTracker(tagName, startTag, tagStart, builder.Consume(), isWellFormed);
                _tagTracker.Push(tracker);
                return;
            }
        }
        else
        {
            // Parsing an end tag.
            var endTagStart = CurrentStart;
            var endTag = ParseEndTag(mode, out var endTagName, out var _);

            Debug.Assert(endTagName != null);
            if (string.Equals(CurrentStartTagName, endTagName, StringComparison.OrdinalIgnoreCase))
            {
                // Happy path. Found a matching start tag. Create the element and reset the builder.
                var tracker = _tagTracker.Pop();
                var element = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag);
                builder.AddRange(tracker.PreviousNodes);
                builder.Add(element);
                return;
            }
            else
            {
                // Current tag scope does not match the end tag. Attempt to recover the start tag
                // by looking up the previous tag scopes for a matching start tag.
                if (!TryRecoverStartTag(builder, endTagName, endTag))
                {
                    // Could not recover.
                    var element = SyntaxFactory.MarkupElement(startTag: null, body: EmptySyntaxList, endTag: endTag);
                    builder.Add(element);

                    if (mode == ParseMode.MarkupInCodeBlock)
                    {
                        CompleteEndTag(builder, endTagName, endTagStart, endTag);
                    }
                }
            }
        }
    }

    private void CompleteEndTag(
        in SyntaxListBuilder<RazorSyntaxNode> builder,
        string endTagName,
        SourceLocation endTagStartLocation,
        MarkupEndTagSyntax endTag)
    {
        // At this point we already know we don't have a matching start tag. Just build whatever is left.
        if (_tagTracker.Count == 0)
        {
            // We can't possibly have a matching start tag.
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                    new SourceSpan(SourceLocationTracker.Advance(endTagStartLocation, "</"), Math.Max(endTagName.Length, 1)), endTagName));
            return;
        }

        while (_tagTracker.Count > 0)
        {
            var tracker = _tagTracker.Pop();
            var unclosedElement = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag: null);
            builder.AddRange(tracker.PreviousNodes);
            builder.Add(unclosedElement);

            if (_tagTracker.Count == 0)
            {
                // This means we couldn't find a match and we're at the outermost start tag. Add an error.
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                        new SourceSpan(
                            SourceLocationTracker.Advance(tracker.TagLocation, "<"),
                            tracker.TagName.Length),
                        tracker.TagName));
            }
        }
    }

    private bool TryRecoverStartTag(in SyntaxListBuilder<RazorSyntaxNode> builder, string endTagName, MarkupEndTagSyntax endTag)
    {
        // First check if the tag we're tracking is a void tag. If so, we need to close it out before moving on.
        while (_tagTracker.Count > 0 &&
            !string.Equals(CurrentStartTagName, endTagName, StringComparison.OrdinalIgnoreCase) &&
            IsVoidElement(CurrentStartTagName))
        {
            var tracker = _tagTracker.Pop();
            var children = builder.Consume();
            var voidElement = SyntaxFactory.MarkupElement(tracker.StartTag, EmptySyntaxList, endTag: null);
            builder.AddRange(tracker.PreviousNodes);
            builder.Add(voidElement);
            builder.AddRange(children);
        }

        var malformedTagCount = 0;
        foreach (var tag in _tagTracker)
        {
            if (string.Equals(tag.TagName, endTagName, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            malformedTagCount++;
        }

        if (malformedTagCount != _tagTracker.Count)
        {
            // This means we found a matching tag.
            for (var i = 0; i < malformedTagCount; i++)
            {
                var tracker = _tagTracker.Pop();
                var malformedElement = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag: null);
                builder.AddRange(tracker.PreviousNodes);
                builder.Add(malformedElement);
            }

            // Now complete our target tag which is not malformed.
            var tagTracker = _tagTracker.Pop();
            var element = SyntaxFactory.MarkupElement(tagTracker.StartTag, builder.Consume(), endTag);
            builder.AddRange(tagTracker.PreviousNodes);
            builder.Add(element);

            return true;
        }

        return false;
    }

    private MarkupStartTagSyntax ParseStartTag(
        ParseMode mode,
        SourceLocation tagStartLocation,
        out string tagName,
        out MarkupTagMode tagMode,
        out bool isWellFormed)
    {
        Assert(SyntaxKind.OpenAngle);

        tagName = string.Empty;
        tagMode = MarkupTagMode.Normal;
        isWellFormed = false;

        var openAngleToken = EatCurrentToken(); // Accept '<'
        var isBangEscape = TryParseBangEscape(out var bangToken);

        if (At(SyntaxKind.Text))
        {
            tagName = CurrentToken.Content;

            if (isBangEscape)
            {
                // We don't want to group <p> and </!p> together.
                tagName = "!" + tagName;
            }
        }

        if (mode == ParseMode.MarkupInCodeBlock &&
            _tagTracker.Count == 0 &&
            string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
        {
            // "<text>" tag is special only if it is the outermost tag.
            return ParseStartTextTag(openAngleToken, out tagMode, out isWellFormed);
        }

        var tagNameToken = At(SyntaxKind.Text) ? EatCurrentToken() : SyntaxFactory.MissingToken(SyntaxKind.Text);

        var attributes = EmptySyntaxList;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var attributeBuilder = pooledResult.Builder;

            // Parse the contents of a tag like attributes.
            ParseAttributes(attributeBuilder);
            attributes = attributeBuilder.ToList();
        }

        SyntaxToken forwardSlashToken = null;
        if (At(SyntaxKind.ForwardSlash))
        {
            // This is a self closing tag.
            tagMode = MarkupTagMode.SelfClosing;
            forwardSlashToken = EatCurrentToken();
        }

        var closeAngleToken = SyntaxFactory.MissingToken(SyntaxKind.CloseAngle);
        if (mode == ParseMode.MarkupInCodeBlock)
        {
            if (EndOfFile || !At(SyntaxKind.CloseAngle))
            {
                // Unfinished tag
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                        new SourceSpan(
                            tagName.Length == 0 ? tagStartLocation : SourceLocationTracker.Advance(tagStartLocation, "<"),
                            Math.Max(tagName.Length, 1)),
                        tagName));
            }
            else
            {
                if (At(SyntaxKind.CloseAngle))
                {
                    isWellFormed = true;
                    closeAngleToken = EatCurrentToken();
                }

                // Completed tags in code blocks have no accepted characters.
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;

                if (tagMode != MarkupTagMode.SelfClosing && IsVoidElement(tagName))
                {
                    // This is a void element.
                    // Technically, void elements like "meta" are not allowed to have end tags. Just in case they do,
                    // we need to look ahead at the next set of tokens.

                    // Place a bookmark
                    var bookmark = CurrentStart.AbsoluteIndex;

                    // Skip whitespace
                    ReadWhile(IsSpacingTokenIncludingNewLines);

                    // Open Angle
                    if (At(SyntaxKind.OpenAngle) && NextIs(SyntaxKind.ForwardSlash))
                    {
                        NextToken();
                        Assert(SyntaxKind.ForwardSlash);
                        NextToken();
                        if (!At(SyntaxKind.Text) || !string.Equals(CurrentToken.Content, tagName, StringComparison.OrdinalIgnoreCase))
                        {
                            // There is no matching end void tag.
                            tagMode = MarkupTagMode.Void;
                        }
                    }
                    else
                    {
                        // There is no matching end void tag.
                        tagMode = MarkupTagMode.Void;
                    }

                    // Go back to the bookmark and just finish this tag at the close angle
                    Context.Source.Position = bookmark;
                    NextToken();
                }
            }
        }
        else if (At(SyntaxKind.CloseAngle))
        {
            isWellFormed = true;
            closeAngleToken = EatCurrentToken();
        }

        // End tag block
        var startTag = SyntaxFactory.MarkupStartTag(openAngleToken, bangToken, tagNameToken, attributes, forwardSlashToken, closeAngleToken);
        if (string.Equals(tagName, ScriptTagName, StringComparison.OrdinalIgnoreCase))
        {
            // If the script tag expects javascript content then we should do minimal parsing until we reach
            // the end script tag. Don't want to incorrectly parse a "var tag = '<input />';" as an HTML tag.
            if (!ScriptTagExpectsHtml(startTag))
            {
                tagMode = MarkupTagMode.Script;
            }
        }

        if (tagNameToken.IsMissing && closeAngleToken.IsMissing)
        {
            // We want to consider tags with no name and no closing angle as invalid.
            // E.g,
            // <, <  @DateTime.Now are all invalid tags
            // <>, < @DateTime.Now>, <strong are all still valid.
            tagMode = MarkupTagMode.Invalid;
        }

        return GetNodeWithSpanContext(startTag);
    }

    private MarkupStartTagSyntax ParseStartTextTag(SyntaxToken openAngleToken, out MarkupTagMode tagMode, out bool isWellFormed)
    {
        // At this point, we should have already accepted the open angle. We won't get here if the tag is escaped.
        tagMode = MarkupTagMode.Normal;
        var textLocation = CurrentStart;
        Assert(SyntaxKind.Text);

        var tagNameToken = EatCurrentToken();

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var miscAttributeContentBuilder = pooledResult.Builder;
            SyntaxToken forwardSlashToken = null;
            SyntaxToken closeAngleToken = null;

            AcceptWhile(IsSpacingToken);
            miscAttributeContentBuilder.Add(OutputAsMarkupLiteral());

            if (At(SyntaxKind.CloseAngle) ||
                (At(SyntaxKind.ForwardSlash) && NextIs(SyntaxKind.CloseAngle)))
            {
                if (At(SyntaxKind.ForwardSlash))
                {
                    tagMode = MarkupTagMode.SelfClosing;
                    forwardSlashToken = EatCurrentToken();
                }

                closeAngleToken = EatCurrentToken();
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }
            else
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                        new SourceSpan(textLocation, contentLength: 4 /* text */)));

                RecoverTextTag(out var miscContent, out closeAngleToken);
                miscAttributeContentBuilder.Add(miscContent);
            }

            isWellFormed = true;
            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            var startTextTag = SyntaxFactory.MarkupStartTag(
                openAngleToken,
                bang: null,
                name: tagNameToken,
                attributes: miscAttributeContentBuilder.ToList(),
                forwardSlash: forwardSlashToken,
                closeAngle: closeAngleToken);

            return GetNodeWithSpanContext(startTextTag).AsMarkupTransition();
        }
    }

    private void RecoverTextTag(out MarkupTextLiteralSyntax miscContent, out SyntaxToken closeAngleToken)
    {
        // We don't want to skip-to and parse because there shouldn't be anything in the body of text tags.
        AcceptUntil(SyntaxKind.CloseAngle, SyntaxKind.NewLine);
        miscContent = OutputAsMarkupLiteral();

        // Include the close angle in the text tag block if it's there, otherwise just move on
        if (At(SyntaxKind.CloseAngle))
        {
            closeAngleToken = EatCurrentToken();
        }
        else
        {
            closeAngleToken = SyntaxFactory.MissingToken(SyntaxKind.CloseAngle);
        }
    }

    private MarkupEndTagSyntax ParseEndTag(ParseMode mode, out string tagName, out bool isWellFormed)
    {
        // This section can accept things like: '</p  >' or '</p>' etc.
        Assert(SyntaxKind.OpenAngle);

        tagName = string.Empty;
        SyntaxToken tagNameToken = null;

        var openAngleToken = EatCurrentToken(); // Accept '<'
        var forwardSlashToken = At(SyntaxKind.ForwardSlash) ? EatCurrentToken() : SyntaxFactory.MissingToken(SyntaxKind.ForwardSlash);

        // Whitespace here is invalid (according to the spec)
        var isBangEscape = TryParseBangEscape(out var bangToken);
        if (At(SyntaxKind.Text))
        {
            tagName = isBangEscape ? "!" : string.Empty;
            tagName += CurrentToken.Content;

            if (mode == ParseMode.MarkupInCodeBlock &&
                string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
            {
                // "<text>" tag is special only if it is the outermost tag. We need to figure out if the current end text tag
                // matches the outermost start text tag.
                var openTextTagCount = 0;
                foreach (var tracker in _tagTracker)
                {
                    if (string.Equals(tracker.TagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        openTextTagCount++;
                    }
                }

                if (openTextTagCount == 1 &&
                    string.Equals(_tagTracker.Last().TagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
                {
                    // This means there is only one open text tag and it is the outermost tag.
                    return ParseEndTextTag(openAngleToken, forwardSlashToken, out isWellFormed);
                }
            }

            tagNameToken = EatCurrentToken();
        }
        else
        {
            tagNameToken = SyntaxFactory.MissingToken(SyntaxKind.Text);
        }

        SyntaxToken closeAngleToken = null;
        MarkupMiscAttributeContentSyntax miscAttributeContent = null;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var miscAttributeBuilder = pooledResult.Builder;

            AcceptWhile(SyntaxKind.Whitespace);
            miscAttributeBuilder.Add(OutputAsMarkupLiteral());

            if (mode == ParseMode.MarkupInCodeBlock)
            {
                // We want to accept malformed end tags as content.
                AcceptUntil(SyntaxKind.CloseAngle, SyntaxKind.OpenAngle);
                miscAttributeBuilder.Add(OutputAsMarkupLiteral());

                if (At(SyntaxKind.CloseAngle))
                {
                    // Completed tags in code blocks have no accepted characters.
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                }
            }

            if (miscAttributeBuilder.Count > 0)
            {
                miscAttributeContent = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeBuilder.ToList());
            }
        }

        if (At(SyntaxKind.CloseAngle))
        {
            isWellFormed = true;
            closeAngleToken = EatCurrentToken();
        }
        else
        {
            isWellFormed = false;
            closeAngleToken = SyntaxFactory.MissingToken(SyntaxKind.CloseAngle);
        }

        // End tag block
        var endTag = SyntaxFactory.MarkupEndTag(openAngleToken, forwardSlashToken, bangToken, tagNameToken, miscAttributeContent, closeAngleToken);
        return GetNodeWithSpanContext(endTag);
    }

    private MarkupEndTagSyntax ParseEndTextTag(SyntaxToken openAngleToken, SyntaxToken forwardSlashToken, out bool isWellFormed)
    {
        // At this point, we should have already accepted the open angle and forward slash. We won't get here if the tag is escaped.
        var textLocation = CurrentStart;
        Assert(SyntaxKind.Text);
        var tagNameToken = EatCurrentToken();

        MarkupMiscAttributeContentSyntax miscAttributeContent = null;
        SyntaxToken closeAngleToken = null;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var miscAttributeBuilder = pooledResult.Builder;

            isWellFormed = At(SyntaxKind.CloseAngle);
            if (!isWellFormed)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                        new SourceSpan(textLocation, contentLength: 4 /* text */)));

                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                RecoverTextTag(out var miscContent, out closeAngleToken);
                miscAttributeBuilder.Add(miscContent);
            }
            else
            {
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                closeAngleToken = EatCurrentToken();
            }

            if (miscAttributeBuilder.Count > 0)
            {
                miscAttributeContent = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeBuilder.ToList());
            }
        }

        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
        var endTextTag = SyntaxFactory.MarkupEndTag(
            openAngleToken,
            forwardSlashToken,
            bang: null,
            name: tagNameToken,
            miscAttributeContent: miscAttributeContent,
            closeAngle: closeAngleToken);
        return GetNodeWithSpanContext(endTextTag).AsMarkupTransition();
    }

    private void ParseAttributes(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (!At(SyntaxKind.Whitespace) && !At(SyntaxKind.NewLine))
        {
            // We should be right after the tag name, so if there's no whitespace or new line, something is wrong
            ParseMiscAttribute(builder);
            return;
        }

        // We are here ($): <tag$ foo="bar" biz="~/Baz" />

        while (!EndOfFile && !IsEndOfTag())
        {
            if (At(SyntaxKind.ForwardSlash))
            {
                // This means we're at a '/' but it's not considered end of tag. E.g. <p / class=foo>
                // We are at the '/' but the tag isn't closed. Accept and continue parsing the next attribute.
                AcceptAndMoveNext();
            }

            ParseAttribute(builder);
        }
    }

    private bool IsEndOfTag()
    {
        if (At(SyntaxKind.ForwardSlash))
        {
            if (NextIs(SyntaxKind.CloseAngle) || NextIs(SyntaxKind.OpenAngle))
            {
                return true;
            }
        }

        return At(SyntaxKind.CloseAngle) || At(SyntaxKind.OpenAngle);
    }

    private void ParseMiscAttribute(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var miscAttributeContentBuilder = pooledResult.Builder;
            while (!EndOfFile)
            {
                ParseMarkupNodes(miscAttributeContentBuilder, ParseMode.Text, IsTagRecoveryStopPoint);
                if (!EndOfFile)
                {
                    EnsureCurrent();
                    switch (CurrentToken.Kind)
                    {
                        case SyntaxKind.SingleQuote:
                        case SyntaxKind.DoubleQuote:
                            // We should parse until we reach a matching quote.
                            var openQuoteKind = CurrentToken.Kind;
                            AcceptAndMoveNext();
                            ParseMarkupNodes(miscAttributeContentBuilder, ParseMode.Text, token => token.Kind == openQuoteKind);
                            if (!EndOfFile)
                            {
                                Assert(openQuoteKind);
                                AcceptAndMoveNext();
                            }
                            break;
                        case SyntaxKind.OpenAngle: // Another "<" means this tag is invalid.
                        case SyntaxKind.ForwardSlash: // Empty tag
                        case SyntaxKind.CloseAngle: // End of tag
                            miscAttributeContentBuilder.Add(OutputAsMarkupLiteral());
                            if (miscAttributeContentBuilder.Count > 0)
                            {
                                var miscAttributeContent = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeContentBuilder.ToList());
                                builder.Add(miscAttributeContent);
                            }
                            return;
                        default:
                            AcceptAndMoveNext();
                            break;
                    }
                }
            }

            miscAttributeContentBuilder.Add(OutputAsMarkupLiteral());
            if (miscAttributeContentBuilder.Count > 0)
            {
                var miscAttributeContent = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeContentBuilder.ToList());
                builder.Add(miscAttributeContent);
            }
        }
    }

    private void ParseAttribute(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Output anything prior to the attribute, in most cases this will be any invalid content after the tag name or a previous attribute:
        // <input| /| checked />. If there is nothing in-between other attributes this will noop.
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var miscAttributeContentBuilder = pooledResult.Builder;
            miscAttributeContentBuilder.Add(OutputAsMarkupLiteral());
            if (miscAttributeContentBuilder.Count > 0)
            {
                var invalidAttributeBlock = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeContentBuilder.ToList());
                builder.Add(invalidAttributeBlock);
            }
        }

        // http://dev.w3.org/html5/spec/tokenization.html#before-attribute-name-state
        // Capture whitespace
        var attributePrefixWhitespace = ReadWhile(static token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);

        // http://dev.w3.org/html5/spec/tokenization.html#attribute-name-state
        // Read the 'name' (i.e. read until the '=' or whitespace/newline)
        if (!TryParseAttributeName(out var nameTokens))
        {
            // Unexpected character in tag, enter recovery
            Accept(attributePrefixWhitespace);
            ParseMiscAttribute(builder);
            return;
        }

        Accept(attributePrefixWhitespace); // Whitespace before attribute name
        var namePrefix = OutputAsMarkupLiteral();
        Accept(nameTokens); // Attribute name
        var name = OutputAsMarkupLiteral();

        var atMinimizedAttribute = !TokenExistsAfterWhitespace(SyntaxKind.Equals);
        if (atMinimizedAttribute)
        {
            // Minimized attribute
            var minimizedAttributeBlock = SyntaxFactory.MarkupMinimizedAttributeBlock(namePrefix, name);
            builder.Add(minimizedAttributeBlock);
        }
        else
        {
            // Not a minimized attribute
            var attributeBlock = ParseRemainingAttribute(namePrefix, name);
            builder.Add(attributeBlock);
        }
    }

    private bool TryParseAttributeName(out IReadOnlyList<SyntaxToken> nameTokens)
    {
        nameTokens = Array.Empty<SyntaxToken>();
        //
        // We are currently here <input |name="..." />
        // If we encounter a transition (@) here, it can be parsed as CSharp or Markup depending on the feature flag.
        // For example, in Components, we want to parse it as Markup so we can support directive attributes.
        //
        if (Context.FeatureFlags.AllowCSharpInMarkupAttributeArea &&
            (At(SyntaxKind.Transition) || At(SyntaxKind.RazorCommentTransition)))
        {
            // If we get here, there is CSharp in the attribute area. Don't try to parse the name.
            return false;
        }

        if (IsValidAttributeNameToken(CurrentToken))
        {
            nameTokens = ReadWhile(
                static (token, self) =>
                    token.Kind != SyntaxKind.Whitespace &&
                    token.Kind != SyntaxKind.NewLine &&
                    token.Kind != SyntaxKind.Equals &&
                    token.Kind != SyntaxKind.CloseAngle &&
                    token.Kind != SyntaxKind.OpenAngle &&
                    (token.Kind != SyntaxKind.ForwardSlash || !self.NextIs(SyntaxKind.CloseAngle)),
                this);

            return true;
        }

        return false;
    }

    private MarkupAttributeBlockSyntax ParseRemainingAttribute(MarkupTextLiteralSyntax namePrefix, MarkupTextLiteralSyntax name)
    {
        // Since this is not a minimized attribute, the whitespace after attribute name belongs to this attribute.
        AcceptWhile(static token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);
        var nameSuffix = OutputAsMarkupLiteral();

        Assert(SyntaxKind.Equals); // We should be at "="
        var equalsToken = EatCurrentToken();

        var whitespaceAfterEquals = ReadWhile(static token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);
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

        // First, determine if this is a 'data-' attribute (since those can't use conditional attributes)
        var nameContent = string.Concat(name.LiteralTokens.Nodes.Select(s => s.Content));
        if (IsConditionalAttributeName(nameContent))
        {
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
                        ParseConditionalAttributeValue(attributeValueBuilder, quote);
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
                valueSuffix = OutputAsMarkupLiteral();
            }
        }
        else if (quote != SyntaxKind.Marker || !whitespaceAfterEquals.Any())
        {
            valuePrefix = OutputAsMarkupLiteral();

            attributeValue = ParseNonConditionalAttributeValue(quote);

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

    private RazorBlockSyntax ParseNonConditionalAttributeValue(SyntaxKind quote)
    {
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var attributeValueBuilder = pooledResult.Builder;
            // Not a "conditional" attribute, so just read the value
            ParseMarkupNodes(attributeValueBuilder, ParseMode.Text, token => IsEndOfAttributeValue(quote, token));

            // Output already accepted tokens if any as markup literal
            var literalValue = OutputAsMarkupLiteral();
            attributeValueBuilder.Add(literalValue);

            // Capture the attribute value (will include everything in-between the attribute's quotes).
            return SyntaxFactory.GenericBlock(attributeValueBuilder.ToList());
        }
    }

    private void ParseConditionalAttributeValue(in SyntaxListBuilder<RazorSyntaxNode> builder, SyntaxKind quote)
    {
        var prefixStart = CurrentStart;
        var prefixTokens = ReadWhile(static token => token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.NewLine);

        if (At(SyntaxKind.Transition))
        {
            if (NextIs(SyntaxKind.Transition))
            {
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
            var valueTokens = ReadWhile(
                static (token, arg) =>
                    // These three conditions find separators which break the attribute value into portions
                    token.Kind != SyntaxKind.Whitespace &&
                    token.Kind != SyntaxKind.NewLine &&
                    token.Kind != SyntaxKind.Transition &&
                    // This condition checks for the end of the attribute value (it repeats some of the checks above
                    // but for now that's ok)
                    !arg.self.IsEndOfAttributeValue(arg.quote, token),
                (self: this, quote));
            Accept(valueTokens);
            var value = OutputAsMarkupLiteral();

            var literalAttributeValue = SyntaxFactory.MarkupLiteralAttributeValue(prefix, value);
            builder.Add(literalAttributeValue);
        }
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

    private void ParseJavascriptAndEndScriptTag(in SyntaxListBuilder<RazorSyntaxNode> builder, MarkupStartTagSyntax startTag, AcceptedCharactersInternal endTagAcceptedCharacters = AcceptedCharactersInternal.Any)
    {
        var previousNodes = builder.Consume();

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

        MarkupEndTagSyntax endTag = null;
        if (seenEndScript)
        {
            var tagStart = CurrentStart;
            builder.Add(OutputAsMarkupLiteral());

            var openAngleToken = EatCurrentToken(); // '<'
            var forwardSlashToken = EatCurrentToken(); // '/'
            var tagNameToken = EatCurrentToken(); // 'script'
            MarkupMiscAttributeContentSyntax miscContent = null;
            SyntaxToken closeAngleToken = null;

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var miscAttributeBuilder = pooledResult.Builder;

                // We want to accept malformed end tags as content.
                AcceptUntil(SyntaxKind.CloseAngle, SyntaxKind.OpenAngle);
                miscAttributeBuilder.Add(OutputAsMarkupLiteral());

                if (miscAttributeBuilder.Count > 0)
                {
                    miscContent = SyntaxFactory.MarkupMiscAttributeContent(miscAttributeBuilder.ToList());
                }

                if (!At(SyntaxKind.CloseAngle))
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                            new SourceSpan(SourceLocationTracker.Advance(tagStart, "</"), ScriptTagName.Length),
                            ScriptTagName));
                    closeAngleToken = SyntaxFactory.MissingToken(SyntaxKind.CloseAngle);
                }
                else
                {
                    closeAngleToken = EatCurrentToken();
                }
            }

            SpanContext.EditHandler.AcceptedCharacters = endTagAcceptedCharacters;

            endTag = SyntaxFactory.MarkupEndTag(
                openAngleToken,
                forwardSlashToken,
                bang: null,
                name: tagNameToken,
                miscAttributeContent: miscContent,
                closeAngle: closeAngleToken);
            endTag = GetNodeWithSpanContext(endTag);
        }

        var element = SyntaxFactory.MarkupElement(startTag, builder.Consume(), endTag);
        builder.AddRange(previousNodes);
        builder.Add(element);
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
        AcceptWhile(IsSpacingTokenIncludingNewLines);
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
            AcceptWhile(IsSpacingToken);
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
        if (Context.NullGenerateWhitespaceAndNewLine)
        {
            // Usually this is set to true when a Code block ends and there is whitespace left after it.
            // We don't want to write it to output.
            Context.NullGenerateWhitespaceAndNewLine = false;
            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            AcceptWhile(IsSpacingToken);
            if (At(SyntaxKind.NewLine))
            {
                AcceptAndMoveNext();
            }

            builder.Add(OutputAsMarkupEphemeralLiteral());
        }

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
            AcceptWhile(IsSpacingToken);
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
            AcceptWhile(IsSpacingToken);
            if (At(SyntaxKind.NewLine))
            {
                AcceptAndMoveNext();
            }

            builder.Add(OutputAsMarkupEphemeralLiteral());
        }

        AcceptWhile(IsSpacingTokenIncludingNewLines);
    }

    private bool ScriptTagExpectsHtml(MarkupStartTagSyntax tagBlock)
    {
        MarkupAttributeBlockSyntax typeAttribute = null;
        for (var i = 0; i < tagBlock.Attributes.Count; i++)
        {
            var node = tagBlock.Attributes[i];

            if (node is MarkupAttributeBlockSyntax attributeBlock &&
                attributeBlock.Value != null &&
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

    private IReadOnlyList<SyntaxToken> FastReadWhitespaceAndNewLines()
    {
        if (EnsureCurrent() && (CurrentToken.Kind == SyntaxKind.Whitespace || CurrentToken.Kind == SyntaxKind.NewLine))
        {
            var whitespaceTokens = new List<SyntaxToken>();

            whitespaceTokens.Add(CurrentToken);
            NextToken();

            while (EnsureCurrent() && (CurrentToken.Kind == SyntaxKind.Whitespace || CurrentToken.Kind == SyntaxKind.NewLine))
            {
                whitespaceTokens.Add(CurrentToken);
                NextToken();
            }

            return whitespaceTokens;
        }

        return Array.Empty<SyntaxToken>();
    }

    private ParserState GetParserState(ParseMode mode)
    {
        var whitespace = FastReadWhitespaceAndNewLines();
        try
        {
            if (whitespace.Count == 0 && EndOfFile)
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
            else if (whitespace.Count > 0)
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
            if (whitespace.Count > 0)
            {
                PutCurrentBack();
                PutBack(whitespace);
                EnsureCurrent();
            }
        }
    }

    private bool TryParseBangEscape(out SyntaxToken bangToken)
    {
        bangToken = null;
        if (IsBangEscape(lookahead: 0))
        {
            // Accept the parser escape character '!'.
            Assert(SyntaxKind.Bang);
            bangToken = EatCurrentToken();

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

    // Internal for testing
    internal bool IsHtmlCommentAhead()
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

    private bool IsConditionalAttributeName(string name)
    {
        if (Context.FeatureFlags.AllowConditionalDataDashAttributes)
        {
            return true;
        }

        if (!name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private void NestingBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Tuple<string, string> nestingSequences)
    {
        var nesting = 1;
        while (nesting > 0 && !EndOfFile)
        {
            ParseMarkupNodes(builder, ParseMode.Text, token =>
                token.Kind == SyntaxKind.Text ||
                token.Kind == SyntaxKind.OpenAngle);
            if (At(SyntaxKind.Text))
            {
                // We need to inspect this text token to figure out if this could be the end of the Razor block
                // or if it is the start of a new block in which case we need to keep track of the nesting level.
                nesting += ProcessTextToken(builder, nestingSequences, nesting);
                if (CurrentToken != null)
                {
                    // This was just some regular text. Accept and move on.
                    // If we were at the end of a block, we would have already accepted it and CurrentToken will be null.
                    AcceptAndMoveNext();
                }
                else if (nesting > 0)
                {
                    // This was the start of a new block. We've already consumed the text. Move on.
                    NextToken();
                }
            }
            else
            {
                // We're at a tag. Parse it and continue.
                ParseMarkupNode(builder, ParseMode.Markup);
            }
        }

        // If we are still tracking any unclosed start tags, we need to close them.
        while (_tagTracker.Count > 0)
        {
            var tracker = _tagTracker.Pop();
            var element = SyntaxFactory.MarkupElement(tracker.StartTag, builder.Consume(), endTag: null);
            builder.AddRange(tracker.PreviousNodes);
            builder.Add(element);
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
            var possibleStart = new StringSegment(CurrentToken.Content, position, sequence.Length);
            if (possibleStart.Equals(sequence, Comparison))
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

    internal static bool IsHyphen(SyntaxToken token)
    {
        return token.Kind == SyntaxKind.Text && token.Content == "-";
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

    private static bool IsTagRecoveryStopPoint(SyntaxToken token)
    {
        return token.Kind == SyntaxKind.CloseAngle ||
               token.Kind == SyntaxKind.ForwardSlash ||
               token.Kind == SyntaxKind.OpenAngle ||
               token.Kind == SyntaxKind.SingleQuote ||
               token.Kind == SyntaxKind.DoubleQuote;
    }

    private void DefaultMarkupSpanContext(SpanContextBuilder spanContext)
    {
        spanContext.ChunkGenerator = new MarkupChunkGenerator();
        spanContext.EditHandler = new SpanEditHandler(LanguageTokenizeString, AcceptedCharactersInternal.Any);
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

    private static bool IsVoidElement(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        if (tagName.StartsWith("!", StringComparison.Ordinal))
        {
            tagName = tagName.Substring(1);
        }

        return ParserHelpers.VoidElements.Contains(tagName);
    }

    private enum ParseMode
    {
        Markup,
        MarkupInCodeBlock,
        Text,
    }

    private enum MarkupTagMode
    {
        Normal,
        Void,
        SelfClosing,
        Script,
        Invalid,
    }

    private class TagTracker
    {
        public TagTracker(
            string tagName,
            MarkupStartTagSyntax startTag,
            SourceLocation tagLocation,
            SyntaxList<RazorSyntaxNode> previousNodes,
            bool isWellFormed)
        {
            TagName = tagName;
            StartTag = startTag;
            TagLocation = tagLocation;
            PreviousNodes = previousNodes;
            IsWellFormed = isWellFormed;
        }

        public string TagName { get; }

        public MarkupStartTagSyntax StartTag { get; }

        public SourceLocation TagLocation { get; }

        public SyntaxList<RazorSyntaxNode> PreviousNodes { get; }

        public bool IsWellFormed { get; }
    }
}
