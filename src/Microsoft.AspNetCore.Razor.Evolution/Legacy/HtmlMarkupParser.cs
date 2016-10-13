// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlMarkupParser : TokenizerBackedParser<HtmlTokenizer, HtmlSymbol, HtmlSymbolType>
    {
        private const string ScriptTagName = "script";

        private static readonly char[] ValidAfterTypeAttributeNameCharacters = { ' ', '\t', '\r', '\n', '\f', '=' };
        private SourceLocation _lastTagStart = SourceLocation.Zero;
        private HtmlSymbol _bufferedOpenAngle;

        //From http://dev.w3.org/html5/spec/Overview.html#elements-0
        private ISet<string> _voidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            : base(HtmlLanguageCharacteristics.Instance, context)
        {
        }

        public ParserBase CodeParser { get; set; }

        public ISet<string> VoidElements
        {
            get { return _voidElements; }
        }

        private bool CaseSensitive { get; set; }

        private StringComparison Comparison
        {
            get { return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        protected override bool SymbolTypeEquals(HtmlSymbolType x, HtmlSymbolType y) => x == y;

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            span.Kind = SpanKind.Markup;
            span.ChunkGenerator = new MarkupChunkGenerator();
            base.BuildSpan(span, start, content);
        }

        protected override void OutputSpanBeforeRazorComment()
        {
            Output(SpanKind.Markup);
        }

        protected void SkipToAndParseCode(HtmlSymbolType type)
        {
            SkipToAndParseCode(sym => sym.Type == type);
        }

        protected void SkipToAndParseCode(Func<HtmlSymbol, bool> condition)
        {
            HtmlSymbol last = null;
            var startOfLine = false;
            while (!EndOfFile && !condition(CurrentSymbol))
            {
                if (Context.NullGenerateWhitespaceAndNewLine)
                {
                    Context.NullGenerateWhitespaceAndNewLine = false;
                    Span.ChunkGenerator = SpanChunkGenerator.Null;
                    AcceptWhile(symbol => symbol.Type == HtmlSymbolType.WhiteSpace);
                    if (At(HtmlSymbolType.NewLine))
                    {
                        AcceptAndMoveNext();
                    }

                    Output(SpanKind.Markup);
                }
                else if (At(HtmlSymbolType.NewLine))
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
                else if (At(HtmlSymbolType.Transition))
                {
                    var transition = CurrentSymbol;
                    NextToken();
                    if (At(HtmlSymbolType.Transition))
                    {
                        if (last != null)
                        {
                            Accept(last);
                            last = null;
                        }
                        Output(SpanKind.Markup);
                        Accept(transition);
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKind.Markup);
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
                        if (!Context.DesignTimeMode && last.Type == HtmlSymbolType.WhiteSpace && startOfLine)
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

                    OtherParserBlock();
                }
                else if (At(HtmlSymbolType.RazorCommentTransition))
                {
                    if (last != null)
                    {
                        // Don't render the whitespace between the start of the line and the razor comment.
                        if (startOfLine && last.Type == HtmlSymbolType.WhiteSpace)
                        {
                            AddMarkerSymbolIfNecessary();
                            // Output the symbols that may have been accepted prior to the whitespace.
                            Output(SpanKind.Markup);

                            Span.ChunkGenerator = SpanChunkGenerator.Null;
                        }

                        Accept(last);
                        last = null;
                    }

                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);

                    RazorComment();

                    // Handle the whitespace and newline at the end of a razor comment.
                    if (startOfLine &&
                        (At(HtmlSymbolType.NewLine) ||
                        (At(HtmlSymbolType.WhiteSpace) && NextIs(HtmlSymbolType.NewLine))))
                    {
                        AcceptWhile(IsSpacingToken(includeNewLines: false));
                        AcceptAndMoveNext();
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKind.Markup);
                    }
                }
                else
                {
                    // As long as we see whitespace, we're still at the "start" of the line
                    startOfLine &= At(HtmlSymbolType.WhiteSpace);

                    // If there's a last token, accept it
                    if (last != null)
                    {
                        Accept(last);
                        last = null;
                    }

                    // Advance
                    last = CurrentSymbol;
                    NextToken();
                }
            }

            if (last != null)
            {
                Accept(last);
            }
        }

        protected static Func<HtmlSymbol, bool> IsSpacingToken(bool includeNewLines)
        {
            return sym => sym.Type == HtmlSymbolType.WhiteSpace || (includeNewLines && sym.Type == HtmlSymbolType.NewLine);
        }

        private void OtherParserBlock()
        {
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.Markup);
            using (PushSpanConfig())
            {
                CodeParser.ParseBlock();
            }
            Initialize(Span);
            NextToken();
        }

        private bool IsBangEscape(int lookahead)
        {
            var potentialBang = Lookahead(lookahead);

            if (potentialBang != null &&
                potentialBang.Type == HtmlSymbolType.Bang)
            {
                var afterBang = Lookahead(lookahead + 1);

                return afterBang != null &&
                    afterBang.Type == HtmlSymbolType.Text &&
                    !string.Equals(afterBang.Content, "DOCTYPE", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void OptionalBangEscape()
        {
            if (IsBangEscape(lookahead: 0))
            {
                Output(SpanKind.Markup);

                // Accept the parser escape character '!'.
                Assert(HtmlSymbolType.Bang);
                AcceptAndMoveNext();

                // Setup the metacode span that we will be outputing.
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKind.MetaCode, AcceptedCharacters.None);
            }
        }

        public override void ParseBlock()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(LegacyResources.Parser_Context_Not_Set);
            }

            using (PushSpanConfig(DefaultMarkupSpan))
            {
                using (Context.Builder.StartBlock(BlockType.Markup))
                {
                    if (!NextToken())
                    {
                        return;
                    }

                    AcceptWhile(IsSpacingToken(includeNewLines: true));

                    if (CurrentSymbol.Type == HtmlSymbolType.OpenAngle)
                    {
                        // "<" => Implicit Tag Block
                        TagBlock(new Stack<Tuple<HtmlSymbol, SourceLocation>>());
                    }
                    else if (CurrentSymbol.Type == HtmlSymbolType.Transition)
                    {
                        // "@" => Explicit Tag/Single Line Block OR Template
                        Output(SpanKind.Markup);

                        // Definitely have a transition span
                        Assert(HtmlSymbolType.Transition);
                        AcceptAndMoveNext();
                        Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKind.Transition);
                        if (At(HtmlSymbolType.Transition))
                        {
                            Span.ChunkGenerator = SpanChunkGenerator.Null;
                            AcceptAndMoveNext();
                            Output(SpanKind.MetaCode);
                        }
                        AfterTransition();
                    }
                    else
                    {
                        Context.ErrorSink.OnError(
                            CurrentSymbol.Start,
                            LegacyResources.ParseError_MarkupBlock_Must_Start_With_Tag,
                            CurrentSymbol.Content.Length);
                    }
                    Output(SpanKind.Markup);
                }
            }
        }

        private void DefaultMarkupSpan(SpanBuilder span)
        {
            span.ChunkGenerator = new MarkupChunkGenerator();
            span.EditHandler = new SpanEditHandler(Language.TokenizeString, AcceptedCharacters.Any);
        }

        private void AfterTransition()
        {
            // "@:" => Explicit Single Line Block
            if (CurrentSymbol.Type == HtmlSymbolType.Text && CurrentSymbol.Content.Length > 0 && CurrentSymbol.Content[0] == ':')
            {
                // Split the token
                Tuple<HtmlSymbol, HtmlSymbol> split = Language.SplitSymbol(CurrentSymbol, 1, HtmlSymbolType.Colon);

                // The first part (left) is added to this span and we return a MetaCode span
                Accept(split.Item1);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKind.MetaCode);
                if (split.Item2 != null)
                {
                    Accept(split.Item2);
                }
                NextToken();
                SingleLineMarkup();
            }
            else if (CurrentSymbol.Type == HtmlSymbolType.OpenAngle)
            {
                TagBlock(new Stack<Tuple<HtmlSymbol, SourceLocation>>());
            }
        }

        private void SingleLineMarkup()
        {
            // Parse until a newline, it's that simple!
            // First, signal to code parser that whitespace is significant to us.
            var old = Context.WhiteSpaceIsSignificantToAncestorBlock;
            Context.WhiteSpaceIsSignificantToAncestorBlock = true;
            Span.EditHandler = new SpanEditHandler(Language.TokenizeString);
            SkipToAndParseCode(HtmlSymbolType.NewLine);
            if (!EndOfFile && CurrentSymbol.Type == HtmlSymbolType.NewLine)
            {
                AcceptAndMoveNext();
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            PutCurrentBack();
            Context.WhiteSpaceIsSignificantToAncestorBlock = old;
            Output(SpanKind.Markup);
        }

        private void TagBlock(Stack<Tuple<HtmlSymbol, SourceLocation>> tags)
        {
            // Skip Whitespace and Text
            var complete = false;
            do
            {
                SkipToAndParseCode(HtmlSymbolType.OpenAngle);

                // Output everything prior to the OpenAngle into a markup span
                Output(SpanKind.Markup);

                // Do not want to start a new tag block if we're at the end of the file.
                IDisposable tagBlockWrapper = null;
                try
                {
                    var atSpecialTag = AtSpecialTag;

                    if (!EndOfFile && !atSpecialTag)
                    {
                        // Start a Block tag.  This is used to wrap things like <p> or <a class="btn"> etc.
                        tagBlockWrapper = Context.Builder.StartBlock(BlockType.Tag);
                    }

                    if (EndOfFile)
                    {
                        EndTagBlock(tags, complete: true);
                    }
                    else
                    {
                        _bufferedOpenAngle = null;
                        _lastTagStart = CurrentLocation;
                        Assert(HtmlSymbolType.OpenAngle);
                        _bufferedOpenAngle = CurrentSymbol;
                        var tagStart = CurrentLocation;
                        if (!NextToken())
                        {
                            Accept(_bufferedOpenAngle);
                            EndTagBlock(tags, complete: false);
                        }
                        else
                        {
                            complete = AfterTagStart(tagStart, tags, atSpecialTag, tagBlockWrapper);
                        }
                    }

                    if (complete)
                    {
                        // Completed tags have no accepted characters inside of blocks.
                        Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                    }

                    // Output the contents of the tag into its own markup span.
                    Output(SpanKind.Markup);
                }
                finally
                {
                    // Will be null if we were at end of file or special tag when initially created.
                    if (tagBlockWrapper != null)
                    {
                        // End tag block
                        tagBlockWrapper.Dispose();
                    }
                }
            }
            while (tags.Count > 0);

            EndTagBlock(tags, complete);
        }

        private bool AfterTagStart(SourceLocation tagStart,
                                   Stack<Tuple<HtmlSymbol, SourceLocation>> tags,
                                   bool atSpecialTag,
                                   IDisposable tagBlockWrapper)
        {
            if (!EndOfFile)
            {
                switch (CurrentSymbol.Type)
                {
                    case HtmlSymbolType.ForwardSlash:
                        // End Tag
                        return EndTag(tagStart, tags, tagBlockWrapper);
                    case HtmlSymbolType.Bang:
                        // Comment, CDATA, DOCTYPE, or a parser-escaped HTML tag.
                        if (atSpecialTag)
                        {
                            Accept(_bufferedOpenAngle);
                            return BangTag();
                        }
                        else
                        {
                            goto default;
                        }
                    case HtmlSymbolType.QuestionMark:
                        // XML PI
                        Accept(_bufferedOpenAngle);
                        return XmlPI();
                    default:
                        // Start Tag
                        return StartTag(tags, tagBlockWrapper);
                }
            }
            if (tags.Count == 0)
            {
                Context.ErrorSink.OnError(
                    CurrentLocation,
                    LegacyResources.ParseError_OuterTagMissingName,
                    length: 1  /* end of file */);
            }
            return false;
        }

        private bool XmlPI()
        {
            // Accept "?"
            Assert(HtmlSymbolType.QuestionMark);
            AcceptAndMoveNext();
            return AcceptUntilAll(HtmlSymbolType.QuestionMark, HtmlSymbolType.CloseAngle);
        }

        private bool BangTag()
        {
            // Accept "!"
            Assert(HtmlSymbolType.Bang);

            if (AcceptAndMoveNext())
            {
                if (CurrentSymbol.Type == HtmlSymbolType.DoubleHyphen)
                {
                    AcceptAndMoveNext();

                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                    while (!EndOfFile)
                    {
                        SkipToAndParseCode(HtmlSymbolType.DoubleHyphen);
                        if (At(HtmlSymbolType.DoubleHyphen))
                        {
                            AcceptWhile(HtmlSymbolType.DoubleHyphen);

                            if (At(HtmlSymbolType.Text) &&
                                string.Equals(CurrentSymbol.Content, "-", StringComparison.Ordinal))
                            {
                                AcceptAndMoveNext();
                            }

                            if (At(HtmlSymbolType.CloseAngle))
                            {
                                AcceptAndMoveNext();
                                return true;
                            }
                        }
                    }

                    return false;
                }
                else if (CurrentSymbol.Type == HtmlSymbolType.LeftBracket)
                {
                    if (AcceptAndMoveNext())
                    {
                        return CData();
                    }
                }
                else
                {
                    AcceptAndMoveNext();
                    return AcceptUntilAll(HtmlSymbolType.CloseAngle);
                }
            }

            return false;
        }

        private bool CData()
        {
            if (CurrentSymbol.Type == HtmlSymbolType.Text && string.Equals(CurrentSymbol.Content, "cdata", StringComparison.OrdinalIgnoreCase))
            {
                if (AcceptAndMoveNext())
                {
                    if (CurrentSymbol.Type == HtmlSymbolType.LeftBracket)
                    {
                        return AcceptUntilAll(HtmlSymbolType.RightBracket, HtmlSymbolType.RightBracket, HtmlSymbolType.CloseAngle);
                    }
                }
            }

            return false;
        }

        private bool EndTag(SourceLocation tagStart,
                            Stack<Tuple<HtmlSymbol, SourceLocation>> tags,
                            IDisposable tagBlockWrapper)
        {
            // Accept "/" and move next
            Assert(HtmlSymbolType.ForwardSlash);
            var forwardSlash = CurrentSymbol;
            if (!NextToken())
            {
                Accept(_bufferedOpenAngle);
                Accept(forwardSlash);
                return false;
            }
            else
            {
                var tagName = string.Empty;
                HtmlSymbol bangSymbol = null;

                if (At(HtmlSymbolType.Bang))
                {
                    bangSymbol = CurrentSymbol;

                    var nextSymbol = Lookahead(count: 1);

                    if (nextSymbol != null && nextSymbol.Type == HtmlSymbolType.Text)
                    {
                        tagName = "!" + nextSymbol.Content;
                    }
                }
                else if (At(HtmlSymbolType.Text))
                {
                    tagName = CurrentSymbol.Content;
                }

                var matched = RemoveTag(tags, tagName, tagStart);

                if (tags.Count == 0 &&
                    // Note tagName may contain a '!' escape character. This ensures </!text> doesn't match here.
                    // </!text> tags are treated like any other escaped HTML end tag.
                    string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) &&
                    matched)
                {
                    return EndTextTag(forwardSlash, tagBlockWrapper);
                }
                Accept(_bufferedOpenAngle);
                Accept(forwardSlash);

                OptionalBangEscape();

                AcceptUntil(HtmlSymbolType.CloseAngle);

                // Accept the ">"
                return Optional(HtmlSymbolType.CloseAngle);
            }
        }

        private void RecoverTextTag()
        {
            // We don't want to skip-to and parse because there shouldn't be anything in the body of text tags.
            AcceptUntil(HtmlSymbolType.CloseAngle, HtmlSymbolType.NewLine);

            // Include the close angle in the text tag block if it's there, otherwise just move on
            Optional(HtmlSymbolType.CloseAngle);
        }

        private bool EndTextTag(HtmlSymbol solidus, IDisposable tagBlockWrapper)
        {
            Accept(_bufferedOpenAngle);
            Accept(solidus);

            var textLocation = CurrentLocation;
            Assert(HtmlSymbolType.Text);
            AcceptAndMoveNext();

            var seenCloseAngle = Optional(HtmlSymbolType.CloseAngle);

            if (!seenCloseAngle)
            {
                Context.ErrorSink.OnError(
                    textLocation,
                    LegacyResources.ParseError_TextTagCannotContainAttributes,
                    length: 4 /* text */);

                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                RecoverTextTag();
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Span.ChunkGenerator = SpanChunkGenerator.Null;

            CompleteTagBlockWithSpan(tagBlockWrapper, Span.EditHandler.AcceptedCharacters, SpanKind.Transition);

            return seenCloseAngle;
        }

        // Special tags include <!--, <!DOCTYPE, <![CDATA and <? tags
        private bool AtSpecialTag
        {
            get
            {
                if (At(HtmlSymbolType.OpenAngle))
                {
                    if (NextIs(HtmlSymbolType.Bang))
                    {
                        return !IsBangEscape(lookahead: 1);
                    }

                    return NextIs(HtmlSymbolType.QuestionMark);
                }

                return false;
            }
        }

        private bool IsTagRecoveryStopPoint(HtmlSymbol sym)
        {
            return sym.Type == HtmlSymbolType.CloseAngle ||
                   sym.Type == HtmlSymbolType.ForwardSlash ||
                   sym.Type == HtmlSymbolType.OpenAngle ||
                   sym.Type == HtmlSymbolType.SingleQuote ||
                   sym.Type == HtmlSymbolType.DoubleQuote;
        }

        private void TagContent()
        {
            if (!At(HtmlSymbolType.WhiteSpace) && !At(HtmlSymbolType.NewLine))
            {
                // We should be right after the tag name, so if there's no whitespace or new line, something is wrong
                RecoverToEndOfTag();
            }
            else
            {
                // We are here ($): <tag$ foo="bar" biz="~/Baz" />
                while (!EndOfFile && !IsEndOfTag())
                {
                    BeforeAttribute();
                }
            }
        }

        private bool IsEndOfTag()
        {
            if (At(HtmlSymbolType.ForwardSlash))
            {
                if (NextIs(HtmlSymbolType.CloseAngle))
                {
                    return true;
                }
                else
                {
                    AcceptAndMoveNext();
                }
            }
            return At(HtmlSymbolType.CloseAngle) || At(HtmlSymbolType.OpenAngle);
        }

        private void BeforeAttribute()
        {
            // http://dev.w3.org/html5/spec/tokenization.html#before-attribute-name-state
            // Capture whitespace
            var whitespace = ReadWhile(sym => sym.Type == HtmlSymbolType.WhiteSpace || sym.Type == HtmlSymbolType.NewLine);

            if (At(HtmlSymbolType.Transition))
            {
                // Transition outside of attribute value => Switch to recovery mode
                Accept(whitespace);
                RecoverToEndOfTag();
                return;
            }

            // http://dev.w3.org/html5/spec/tokenization.html#attribute-name-state
            // Read the 'name' (i.e. read until the '=' or whitespace/newline)
            var name = Enumerable.Empty<HtmlSymbol>();
            var whitespaceAfterAttributeName = Enumerable.Empty<HtmlSymbol>();
            if (IsValidAttributeNameSymbol(CurrentSymbol))
            {
                name = ReadWhile(sym =>
                                 sym.Type != HtmlSymbolType.WhiteSpace &&
                                 sym.Type != HtmlSymbolType.NewLine &&
                                 sym.Type != HtmlSymbolType.Equals &&
                                 sym.Type != HtmlSymbolType.CloseAngle &&
                                 sym.Type != HtmlSymbolType.OpenAngle &&
                                 (sym.Type != HtmlSymbolType.ForwardSlash || !NextIs(HtmlSymbolType.CloseAngle)));

                // capture whitespace after attribute name (if any)
                whitespaceAfterAttributeName = ReadWhile(
                    sym => sym.Type == HtmlSymbolType.WhiteSpace || sym.Type == HtmlSymbolType.NewLine);
            }
            else
            {
                // Unexpected character in tag, enter recovery
                Accept(whitespace);
                RecoverToEndOfTag();
                return;
            }

            if (!At(HtmlSymbolType.Equals))
            {
                // Minimized attribute

                // We are at the prefix of the next attribute or the end of tag. Put it back so it is parsed later.
                PutCurrentBack();
                PutBack(whitespaceAfterAttributeName);

                // Output anything prior to the attribute, in most cases this will be the tag name:
                // |<input| checked />. If in-between other attributes this will noop or output malformed attribute
                // content (if the previous attribute was malformed).
                Output(SpanKind.Markup);

                using (Context.Builder.StartBlock(BlockType.Markup))
                {
                    Accept(whitespace);
                    Accept(name);
                    Output(SpanKind.Markup);
                }

                return;
            }

            // Not a minimized attribute, parse as if it were well-formed (if attribute turns out to be malformed we
            // will go into recovery).
            Output(SpanKind.Markup);

            // Start a new markup block for the attribute
            using (Context.Builder.StartBlock(BlockType.Markup))
            {
                AttributePrefix(whitespace, name, whitespaceAfterAttributeName);
            }
        }

        private void AttributePrefix(
            IEnumerable<HtmlSymbol> whitespace,
            IEnumerable<HtmlSymbol> nameSymbols,
            IEnumerable<HtmlSymbol> whitespaceAfterAttributeName)
        {
            // First, determine if this is a 'data-' attribute (since those can't use conditional attributes)
            var name = nameSymbols.GetContent(Span.Start);
            var attributeCanBeConditional = !name.Value.StartsWith("data-", StringComparison.OrdinalIgnoreCase);

            // Accept the whitespace and name
            Accept(whitespace);
            Accept(nameSymbols);

            // Since this is not a minimized attribute, the whitespace after attribute name belongs to this attribute.
            Accept(whitespaceAfterAttributeName);
            Assert(HtmlSymbolType.Equals); // We should be at "="
            AcceptAndMoveNext();

            var whitespaceAfterEquals = ReadWhile(sym => sym.Type == HtmlSymbolType.WhiteSpace || sym.Type == HtmlSymbolType.NewLine);
            var quote = HtmlSymbolType.Unknown;
            if (At(HtmlSymbolType.SingleQuote) || At(HtmlSymbolType.DoubleQuote))
            {
                // Found a quote, the whitespace belongs to this attribute.
                Accept(whitespaceAfterEquals);
                quote = CurrentSymbol.Type;
                AcceptAndMoveNext();
            }
            else if (whitespaceAfterEquals.Any())
            {
                // No quotes found after the whitespace. Put it back so that it can be parsed later.
                PutCurrentBack();
                PutBack(whitespaceAfterEquals);
            }

            // We now have the prefix: (i.e. '      foo="')
            var prefix = Span.GetContent();

            if (attributeCanBeConditional)
            {
                Span.ChunkGenerator = SpanChunkGenerator.Null; // The block chunk generator will render the prefix
                Output(SpanKind.Markup);

                // Read the attribute value only if the value is quoted
                // or if there is no whitespace between '=' and the unquoted value.
                if (quote != HtmlSymbolType.Unknown || !whitespaceAfterEquals.Any())
                {
                    // Read the attribute value.
                    while (!EndOfFile && !IsEndOfAttributeValue(quote, CurrentSymbol))
                    {
                        AttributeValue(quote);
                    }
                }

                // Capture the suffix
                var suffix = new LocationTagged<string>(string.Empty, CurrentLocation);
                if (quote != HtmlSymbolType.Unknown && At(quote))
                {
                    suffix = CurrentSymbol.GetContent();
                    AcceptAndMoveNext();
                }

                if (Span.Symbols.Count > 0)
                {
                    // Again, block chunk generator will render the suffix
                    Span.ChunkGenerator = SpanChunkGenerator.Null;
                    Output(SpanKind.Markup);
                }

                // Create the block chunk generator
                Context.Builder.CurrentBlock.ChunkGenerator = new AttributeBlockChunkGenerator(
                    name, prefix, suffix);
            }
            else
            {
                // Output the attribute name, the equals and optional quote. Ex: foo="
                Output(SpanKind.Markup);

                if (quote == HtmlSymbolType.Unknown && whitespaceAfterEquals.Any())
                {
                    return;
                }

                // Not a "conditional" attribute, so just read the value
                SkipToAndParseCode(sym => IsEndOfAttributeValue(quote, sym));

                // Output the attribute value (will include everything in-between the attribute's quotes).
                Output(SpanKind.Markup);

                if (quote != HtmlSymbolType.Unknown)
                {
                    Optional(quote);
                }
                Output(SpanKind.Markup);
            }
        }

        private void AttributeValue(HtmlSymbolType quote)
        {
            var prefixStart = CurrentLocation;
            var prefix = ReadWhile(sym => sym.Type == HtmlSymbolType.WhiteSpace || sym.Type == HtmlSymbolType.NewLine);

            if (At(HtmlSymbolType.Transition))
            {
                if (NextIs(HtmlSymbolType.Transition))
                {
                    // Wrapping this in a block so that the ConditionalAttributeCollapser doesn't rewrite it.
                    using (Context.Builder.StartBlock(BlockType.Markup))
                    {
                        Accept(prefix);

                        // Render a single "@" in place of "@@".
                        Span.ChunkGenerator = new LiteralAttributeChunkGenerator(
                            prefix.GetContent(prefixStart),
                            new LocationTagged<string>(CurrentSymbol.GetContent(), CurrentLocation));
                        AcceptAndMoveNext();
                        Output(SpanKind.Markup, AcceptedCharacters.None);

                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        AcceptAndMoveNext();
                        Output(SpanKind.Markup, AcceptedCharacters.None);
                    }
                }
                else
                {
                    Accept(prefix);
                    var valueStart = CurrentLocation;
                    PutCurrentBack();

                    // Output the prefix but as a null-span. DynamicAttributeBlockChunkGenerator will render it
                    Span.ChunkGenerator = SpanChunkGenerator.Null;

                    // Dynamic value, start a new block and set the chunk generator
                    using (Context.Builder.StartBlock(BlockType.Markup))
                    {
                        Context.Builder.CurrentBlock.ChunkGenerator =
                            new DynamicAttributeBlockChunkGenerator(prefix.GetContent(prefixStart), valueStart);

                        OtherParserBlock();
                    }
                }
            }
            else
            {
                Accept(prefix);

                // Literal value
                // 'quote' should be "Unknown" if not quoted and symbols coming from the tokenizer should never have
                // "Unknown" type.
                var value = ReadWhile(sym =>
                    // These three conditions find separators which break the attribute value into portions
                    sym.Type != HtmlSymbolType.WhiteSpace &&
                    sym.Type != HtmlSymbolType.NewLine &&
                    sym.Type != HtmlSymbolType.Transition &&
                    // This condition checks for the end of the attribute value (it repeats some of the checks above
                    // but for now that's ok)
                    !IsEndOfAttributeValue(quote, sym));
                Accept(value);
                Span.ChunkGenerator = new LiteralAttributeChunkGenerator(
                    prefix.GetContent(prefixStart),
                    value.GetContent(prefixStart));
            }
            Output(SpanKind.Markup);
        }

        private bool IsEndOfAttributeValue(HtmlSymbolType quote, HtmlSymbol sym)
        {
            return EndOfFile || sym == null ||
                   (quote != HtmlSymbolType.Unknown
                        ? sym.Type == quote // If quoted, just wait for the quote
                        : IsUnquotedEndOfAttributeValue(sym));
        }

        private bool IsUnquotedEndOfAttributeValue(HtmlSymbol sym)
        {
            // If unquoted, we have a larger set of terminating characters:
            // http://dev.w3.org/html5/spec/tokenization.html#attribute-value-unquoted-state
            // Also we need to detect "/" and ">"
            return sym.Type == HtmlSymbolType.DoubleQuote ||
                   sym.Type == HtmlSymbolType.SingleQuote ||
                   sym.Type == HtmlSymbolType.OpenAngle ||
                   sym.Type == HtmlSymbolType.Equals ||
                   (sym.Type == HtmlSymbolType.ForwardSlash && NextIs(HtmlSymbolType.CloseAngle)) ||
                   sym.Type == HtmlSymbolType.CloseAngle ||
                   sym.Type == HtmlSymbolType.WhiteSpace ||
                   sym.Type == HtmlSymbolType.NewLine;
        }

        private void RecoverToEndOfTag()
        {
            // Accept until ">", "/" or "<", but parse code
            while (!EndOfFile)
            {
                SkipToAndParseCode(IsTagRecoveryStopPoint);
                if (!EndOfFile)
                {
                    EnsureCurrent();
                    switch (CurrentSymbol.Type)
                    {
                        case HtmlSymbolType.SingleQuote:
                        case HtmlSymbolType.DoubleQuote:
                            ParseQuoted();
                            break;
                        case HtmlSymbolType.OpenAngle:
                        // Another "<" means this tag is invalid.
                        case HtmlSymbolType.ForwardSlash:
                        // Empty tag
                        case HtmlSymbolType.CloseAngle:
                            // End of tag
                            return;
                        default:
                            AcceptAndMoveNext();
                            break;
                    }
                }
            }
        }

        private void ParseQuoted()
        {
            var type = CurrentSymbol.Type;
            AcceptAndMoveNext();
            ParseQuoted(type);
        }

        private void ParseQuoted(HtmlSymbolType type)
        {
            SkipToAndParseCode(type);
            if (!EndOfFile)
            {
                Assert(type);
                AcceptAndMoveNext();
            }
        }

        private bool StartTag(Stack<Tuple<HtmlSymbol, SourceLocation>> tags, IDisposable tagBlockWrapper)
        {
            HtmlSymbol bangSymbol = null;
            HtmlSymbol potentialTagNameSymbol;

            if (At(HtmlSymbolType.Bang))
            {
                bangSymbol = CurrentSymbol;

                potentialTagNameSymbol = Lookahead(count: 1);
            }
            else
            {
                potentialTagNameSymbol = CurrentSymbol;
            }

            HtmlSymbol tagName;

            if (potentialTagNameSymbol == null || potentialTagNameSymbol.Type != HtmlSymbolType.Text)
            {
                tagName = new HtmlSymbol(potentialTagNameSymbol.Start, string.Empty, HtmlSymbolType.Unknown);
            }
            else if (bangSymbol != null)
            {
                tagName = new HtmlSymbol(bangSymbol.Start, "!" + potentialTagNameSymbol.Content, HtmlSymbolType.Text);
            }
            else
            {
                tagName = potentialTagNameSymbol;
            }

            Tuple<HtmlSymbol, SourceLocation> tag = Tuple.Create(tagName, _lastTagStart);

            if (tags.Count == 0 &&
                // Note tagName may contain a '!' escape character. This ensures <!text> doesn't match here.
                // <!text> tags are treated like any other escaped HTML start tag.
                string.Equals(tag.Item1.Content, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
            {
                Output(SpanKind.Markup);
                Span.ChunkGenerator = SpanChunkGenerator.Null;

                Accept(_bufferedOpenAngle);
                var textLocation = CurrentLocation;
                Assert(HtmlSymbolType.Text);

                AcceptAndMoveNext();

                var bookmark = CurrentLocation.AbsoluteIndex;
                IEnumerable<HtmlSymbol> tokens = ReadWhile(IsSpacingToken(includeNewLines: true));
                var empty = At(HtmlSymbolType.ForwardSlash);
                if (empty)
                {
                    Accept(tokens);
                    Assert(HtmlSymbolType.ForwardSlash);
                    AcceptAndMoveNext();
                    bookmark = CurrentLocation.AbsoluteIndex;
                    tokens = ReadWhile(IsSpacingToken(includeNewLines: true));
                }

                if (!Optional(HtmlSymbolType.CloseAngle))
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                    Context.ErrorSink.OnError(
                        textLocation,
                        LegacyResources.ParseError_TextTagCannotContainAttributes,
                        length: 4 /* text */);

                    RecoverTextTag();
                }
                else
                {
                    Accept(tokens);
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                }

                if (!empty)
                {
                    tags.Push(tag);
                }

                CompleteTagBlockWithSpan(tagBlockWrapper, Span.EditHandler.AcceptedCharacters, SpanKind.Transition);

                return true;
            }

            Accept(_bufferedOpenAngle);
            OptionalBangEscape();
            Optional(HtmlSymbolType.Text);
            return RestOfTag(tag, tags, tagBlockWrapper);
        }

        private bool RestOfTag(Tuple<HtmlSymbol, SourceLocation> tag,
                               Stack<Tuple<HtmlSymbol, SourceLocation>> tags,
                               IDisposable tagBlockWrapper)
        {
            TagContent();

            // We are now at a possible end of the tag
            // Found '<', so we just abort this tag.
            if (At(HtmlSymbolType.OpenAngle))
            {
                return false;
            }

            var isEmpty = At(HtmlSymbolType.ForwardSlash);
            // Found a solidus, so don't accept it but DON'T push the tag to the stack
            if (isEmpty)
            {
                AcceptAndMoveNext();
            }

            // Check for the '>' to determine if the tag is finished
            var seenClose = Optional(HtmlSymbolType.CloseAngle);
            if (!seenClose)
            {
                Context.ErrorSink.OnError(
                    SourceLocation.Advance(tag.Item2, "<"),
                    LegacyResources.FormatParseError_UnfinishedTag(tag.Item1.Content),
                    Math.Max(tag.Item1.Content.Length, 1));
            }
            else
            {
                if (!isEmpty)
                {
                    // Is this a void element?
                    var tagName = tag.Item1.Content.Trim();
                    if (VoidElements.Contains(tagName))
                    {
                        CompleteTagBlockWithSpan(tagBlockWrapper, AcceptedCharacters.None, SpanKind.Markup);

                        // Technically, void elements like "meta" are not allowed to have end tags. Just in case they do,
                        // we need to look ahead at the next set of tokens. If we see "<", "/", tag name, accept it and the ">" following it
                        // Place a bookmark
                        var bookmark = CurrentLocation.AbsoluteIndex;

                        // Skip whitespace
                        IEnumerable<HtmlSymbol> whiteSpace = ReadWhile(IsSpacingToken(includeNewLines: true));

                        // Open Angle
                        if (At(HtmlSymbolType.OpenAngle) && NextIs(HtmlSymbolType.ForwardSlash))
                        {
                            var openAngle = CurrentSymbol;
                            NextToken();
                            Assert(HtmlSymbolType.ForwardSlash);
                            var solidus = CurrentSymbol;
                            NextToken();
                            if (At(HtmlSymbolType.Text) && string.Equals(CurrentSymbol.Content, tagName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Accept up to here
                                Accept(whiteSpace);
                                Output(SpanKind.Markup); // Output the whitespace

                                using (Context.Builder.StartBlock(BlockType.Tag))
                                {
                                    Accept(openAngle);
                                    Accept(solidus);
                                    AcceptAndMoveNext();

                                    // Accept to '>', '<' or EOF
                                    AcceptUntil(HtmlSymbolType.CloseAngle, HtmlSymbolType.OpenAngle);
                                    // Accept the '>' if we saw it. And if we do see it, we're complete
                                    var complete = Optional(HtmlSymbolType.CloseAngle);

                                    if (complete)
                                    {
                                        Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                                    }

                                    // Output the closing void element
                                    Output(SpanKind.Markup);

                                    return complete;
                                }
                            }
                        }

                        // Go back to the bookmark and just finish this tag at the close angle
                        Context.Source.Position = bookmark;
                        NextToken();
                    }
                    else if (string.Equals(tagName, ScriptTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!CurrentScriptTagExpectsHtml())
                        {
                            CompleteTagBlockWithSpan(tagBlockWrapper, AcceptedCharacters.None, SpanKind.Markup);

                            SkipToEndScriptAndParseCode(endTagAcceptedCharacters: AcceptedCharacters.None);
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
            return seenClose;
        }

        private void SkipToEndScriptAndParseCode(AcceptedCharacters endTagAcceptedCharacters = AcceptedCharacters.Any)
        {
            // Special case for <script>: Skip to end of script tag and parse code
            var seenEndScript = false;

            while (!seenEndScript && !EndOfFile)
            {
                SkipToAndParseCode(HtmlSymbolType.OpenAngle);
                var tagStart = CurrentLocation;

                if (NextIs(HtmlSymbolType.ForwardSlash))
                {
                    var openAngle = CurrentSymbol;
                    NextToken(); // Skip over '<', current is '/'
                    var solidus = CurrentSymbol;
                    NextToken(); // Skip over '/', current should be text

                    if (At(HtmlSymbolType.Text) &&
                        string.Equals(CurrentSymbol.Content, ScriptTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        seenEndScript = true;
                    }

                    // We put everything back because we just wanted to look ahead to see if the current end tag that we're parsing is
                    // the script tag.  If so we'll generate correct code to encompass it.
                    PutCurrentBack(); // Put back whatever was after the solidus
                    PutBack(solidus); // Put back '/'
                    PutBack(openAngle); // Put back '<'

                    // We just looked ahead, this NextToken will set CurrentSymbol to an open angle bracket.
                    NextToken();
                }

                if (seenEndScript)
                {
                    Output(SpanKind.Markup);

                    using (Context.Builder.StartBlock(BlockType.Tag))
                    {
                        Span.EditHandler.AcceptedCharacters = endTagAcceptedCharacters;

                        AcceptAndMoveNext(); // '<'
                        AcceptAndMoveNext(); // '/'
                        SkipToAndParseCode(HtmlSymbolType.CloseAngle);
                        if (!Optional(HtmlSymbolType.CloseAngle))
                        {
                            Context.ErrorSink.OnError(
                                SourceLocation.Advance(tagStart, "</"),
                                LegacyResources.FormatParseError_UnfinishedTag(ScriptTagName),
                                ScriptTagName.Length);
                        }
                        Output(SpanKind.Markup);
                    }
                }
                else
                {
                    AcceptAndMoveNext(); // Accept '<' (not the closing script tag's open angle)
                }
            }
        }

        private void CompleteTagBlockWithSpan(IDisposable tagBlockWrapper,
                                              AcceptedCharacters acceptedCharacters,
                                              SpanKind spanKind)
        {
            Debug.Assert(tagBlockWrapper != null,
                "Tag block wrapper should not be null when attempting to complete a block");

            Span.EditHandler.AcceptedCharacters = acceptedCharacters;
            // Write out the current span into the block before closing it.
            Output(spanKind);
            // Finish the tag block
            tagBlockWrapper.Dispose();
        }

        private bool AcceptUntilAll(params HtmlSymbolType[] endSequence)
        {
            while (!EndOfFile)
            {
                SkipToAndParseCode(endSequence[0]);
                if (AcceptAll(endSequence))
                {
                    return true;
                }
            }
            Debug.Assert(EndOfFile);
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
            return false;
        }

        private bool RemoveTag(Stack<Tuple<HtmlSymbol, SourceLocation>> tags, string tagName, SourceLocation tagStart)
        {
            Tuple<HtmlSymbol, SourceLocation> currentTag = null;
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
                    SourceLocation.Advance(currentTag.Item2, "<"),
                    LegacyResources.FormatParseError_MissingEndTag(currentTag.Item1.Content),
                    currentTag.Item1.Content.Length);
            }
            else
            {
                Context.ErrorSink.OnError(
                    SourceLocation.Advance(tagStart, "</"),
                    LegacyResources.FormatParseError_UnexpectedEndTag(tagName),
                    tagName.Length);
            }
            return false;
        }

        private void EndTagBlock(Stack<Tuple<HtmlSymbol, SourceLocation>> tags, bool complete)
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
                    SourceLocation.Advance(tag.Item2, "<"),
                    LegacyResources.FormatParseError_MissingEndTag(tag.Item1.Content),
                    tag.Item1.Content.Length);
            }
            else if (complete)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            tags.Clear();
            if (!Context.DesignTimeMode)
            {
                var shouldAcceptWhitespaceAndNewLine = true;

                if (Context.Builder.LastSpan.Kind == SpanKind.Transition)
                {
                    var symbols = ReadWhile(
                        f => (f.Type == HtmlSymbolType.WhiteSpace) || (f.Type == HtmlSymbolType.NewLine));

                    // Make sure the current symbol is not markup, which can be html start tag or @:
                    if (!(At(HtmlSymbolType.OpenAngle) ||
                        (At(HtmlSymbolType.Transition) && Lookahead(count: 1).Content.StartsWith(":"))))
                    {
                        // Don't accept whitespace as markup if the end text tag is followed by csharp.
                        shouldAcceptWhitespaceAndNewLine = false;
                    }

                    PutCurrentBack();
                    PutBack(symbols);
                    EnsureCurrent();
                }

                if (shouldAcceptWhitespaceAndNewLine)
                {
                    // Accept whitespace and a single newline if present
                    AcceptWhile(HtmlSymbolType.WhiteSpace);
                    Optional(HtmlSymbolType.NewLine);
                }
            }
            else if (Span.EditHandler.AcceptedCharacters == AcceptedCharacters.Any)
            {
                AcceptWhile(HtmlSymbolType.WhiteSpace);
                Optional(HtmlSymbolType.NewLine);
            }
            PutCurrentBack();

            if (!complete)
            {
                AddMarkerSymbolIfNecessary();
            }
            Output(SpanKind.Markup);
        }

        internal static bool IsValidAttributeNameSymbol(HtmlSymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }

            // These restrictions cover most of the spec defined: http://www.w3.org/TR/html5/syntax.html#attributes-0
            // However, it's not all of it. For instance we don't special case control characters or allow OpenAngle.
            // It also doesn't try to exclude Razor specific features such as the @ transition. This is based on the
            // expectation that the parser handles such scenarios prior to falling through to name resolution.
            var symbolType = symbol.Type;
            return symbolType != HtmlSymbolType.WhiteSpace &&
                symbolType != HtmlSymbolType.NewLine &&
                symbolType != HtmlSymbolType.CloseAngle &&
                symbolType != HtmlSymbolType.OpenAngle &&
                symbolType != HtmlSymbolType.ForwardSlash &&
                symbolType != HtmlSymbolType.DoubleQuote &&
                symbolType != HtmlSymbolType.SingleQuote &&
                symbolType != HtmlSymbolType.Equals &&
                symbolType != HtmlSymbolType.Unknown;
        }

        public void ParseDocument()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(LegacyResources.Parser_Context_Not_Set);
            }

            using (PushSpanConfig(DefaultMarkupSpan))
            {
                using (Context.Builder.StartBlock(BlockType.Markup))
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
                var tagBlock = Context.Builder.StartBlock(BlockType.Tag);

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
            var blockBuilder = Context.Builder.CurrentBlock;

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

        public void ParseSection(Tuple<string, string> nestingSequences, bool caseSensitive)
        {
            if (Context == null)
            {
                throw new InvalidOperationException(LegacyResources.Parser_Context_Not_Set);
            }

            using (PushSpanConfig(DefaultMarkupSpan))
            {
                using (Context.Builder.StartBlock(BlockType.Markup))
                {
                    NextToken();
                    CaseSensitive = caseSensitive;
                    if (nestingSequences.Item1 == null)
                    {
                        NonNestingSection(nestingSequences.Item2.Split());
                    }
                    else
                    {
                        NestingSection(nestingSequences);
                    }
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Markup);
                }
            }
        }

        private void NonNestingSection(string[] nestingSequenceComponents)
        {
            do
            {
                SkipToAndParseCode(sym => sym.Type == HtmlSymbolType.OpenAngle || AtEnd(nestingSequenceComponents));
                ScanTagInDocumentContext();
                if (!EndOfFile && AtEnd(nestingSequenceComponents))
                {
                    break;
                }
            }
            while (!EndOfFile);

            PutCurrentBack();
        }

        private void NestingSection(Tuple<string, string> nestingSequences)
        {
            var nesting = 1;
            while (nesting > 0 && !EndOfFile)
            {
                SkipToAndParseCode(sym =>
                    sym.Type == HtmlSymbolType.Text ||
                    sym.Type == HtmlSymbolType.OpenAngle);
                if (At(HtmlSymbolType.Text))
                {
                    nesting += ProcessTextToken(nestingSequences, nesting);
                    if (CurrentSymbol != null)
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
                    ScanTagInDocumentContext();
                }
            }
        }

        private bool AtEnd(string[] nestingSequenceComponents)
        {
            EnsureCurrent();
            if (string.Equals(CurrentSymbol.Content, nestingSequenceComponents[0], Comparison))
            {
                var bookmark = CurrentSymbol.Start.AbsoluteIndex;
                try
                {
                    foreach (string component in nestingSequenceComponents)
                    {
                        if (!EndOfFile && !string.Equals(CurrentSymbol.Content, component, Comparison))
                        {
                            return false;
                        }
                        NextToken();
                        while (!EndOfFile && IsSpacingToken(includeNewLines: true)(CurrentSymbol))
                        {
                            NextToken();
                        }
                    }
                    return true;
                }
                finally
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                }
            }
            return false;
        }

        private int ProcessTextToken(Tuple<string, string> nestingSequences, int currentNesting)
        {
            for (int i = 0; i < CurrentSymbol.Content.Length; i++)
            {
                var nestingDelta = HandleNestingSequence(nestingSequences.Item1, i, currentNesting, 1);
                if (nestingDelta == 0)
                {
                    nestingDelta = HandleNestingSequence(nestingSequences.Item2, i, currentNesting, -1);
                }

                if (nestingDelta != 0)
                {
                    return nestingDelta;
                }
            }
            return 0;
        }

        private int HandleNestingSequence(string sequence, int position, int currentNesting, int retIfMatched)
        {
            if (sequence != null &&
                CurrentSymbol.Content[position] == sequence[0] &&
                position + sequence.Length <= CurrentSymbol.Content.Length)
            {
                var possibleStart = CurrentSymbol.Content.Substring(position, sequence.Length);
                if (string.Equals(possibleStart, sequence, Comparison))
                {
                    // Capture the current symbol and "put it back" (really we just want to clear CurrentSymbol)
                    var bookmark = Context.Source.Position;
                    var sym = CurrentSymbol;
                    PutCurrentBack();

                    // Carve up the symbol
                    Tuple<HtmlSymbol, HtmlSymbol> pair = Language.SplitSymbol(sym, position, HtmlSymbolType.Text);
                    var preSequence = pair.Item1;
                    Debug.Assert(pair.Item2 != null);
                    pair = Language.SplitSymbol(pair.Item2, sequence.Length, HtmlSymbolType.Text);
                    var sequenceToken = pair.Item1;
                    var postSequence = pair.Item2;

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
                        Context.Source.Position = sequenceToken.Start.AbsoluteIndex;
                    }
                    else
                    {
                        // This isn't the end of the last nesting sequence, accept the token and keep going
                        Accept(sequenceToken);

                        // Position at the start of the postSequence symbol
                        if (postSequence != null)
                        {
                            Context.Source.Position = postSequence.Start.AbsoluteIndex;
                        }
                        else
                        {
                            Context.Source.Position = bookmark;
                        }
                    }

                    // Return the value we were asked to return if matched, since we found a nesting sequence
                    return retIfMatched;
                }
            }
            return 0;
        }
    }
}
