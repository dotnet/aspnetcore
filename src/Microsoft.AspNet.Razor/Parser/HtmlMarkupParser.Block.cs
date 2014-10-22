// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class HtmlMarkupParser
    {
        private SourceLocation _lastTagStart = SourceLocation.Zero;
        private HtmlSymbol _bufferedOpenAngle;

        public override void ParseBlock()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(RazorResources.Parser_Context_Not_Set);
            }

            using (PushSpanConfig(DefaultMarkupSpan))
            {
                using (Context.StartBlock(BlockType.Markup))
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
                        Span.CodeGenerator = SpanCodeGenerator.Null;
                        Output(SpanKind.Transition);
                        if (At(HtmlSymbolType.Transition))
                        {
                            Span.CodeGenerator = SpanCodeGenerator.Null;
                            AcceptAndMoveNext();
                            Output(SpanKind.MetaCode);
                        }
                        AfterTransition();
                    }
                    else
                    {
                        Context.OnError(CurrentSymbol.Start, RazorResources.ParseError_MarkupBlock_Must_Start_With_Tag);
                    }
                    Output(SpanKind.Markup);
                }
            }
        }

        private void DefaultMarkupSpan(SpanBuilder span)
        {
            span.CodeGenerator = new MarkupCodeGenerator();
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
                Span.CodeGenerator = SpanCodeGenerator.Null;
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
            Span.EditHandler = new SingleLineMarkupEditHandler(Language.TokenizeString);
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
                    if (!EndOfFile && !AtSpecialTag)
                    {
                        // Start a Block tag.  This is used to wrap things like <p> or <a class="btn"> etc.
                        tagBlockWrapper = Context.StartBlock(BlockType.Tag);
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
                            complete = AfterTagStart(tagStart, tags, tagBlockWrapper);
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
                        // Comment
                        Accept(_bufferedOpenAngle);
                        return BangTag();
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
                Context.OnError(CurrentLocation, RazorResources.ParseError_OuterTagMissingName);
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
                    return AcceptUntilAll(HtmlSymbolType.DoubleHyphen, HtmlSymbolType.CloseAngle);
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
            if (CurrentSymbol.Type == HtmlSymbolType.Text && String.Equals(CurrentSymbol.Content, "cdata", StringComparison.OrdinalIgnoreCase))
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
            var solidus = CurrentSymbol;
            if (!NextToken())
            {
                Accept(_bufferedOpenAngle);
                Accept(solidus);
                return false;
            }
            else
            {
                var tagName = String.Empty;
                if (At(HtmlSymbolType.Text))
                {
                    tagName = CurrentSymbol.Content;
                }
                var matched = RemoveTag(tags, tagName, tagStart);

                if (tags.Count == 0 &&
                    String.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) &&
                    matched)
                {
                    return EndTextTag(solidus, tagBlockWrapper);
                }
                Accept(_bufferedOpenAngle);
                Accept(solidus);

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
            var start = _bufferedOpenAngle.Start;

            Accept(_bufferedOpenAngle);
            Accept(solidus);

            Assert(HtmlSymbolType.Text);
            AcceptAndMoveNext();

            var seenCloseAngle = Optional(HtmlSymbolType.CloseAngle);

            if (!seenCloseAngle)
            {
                Context.OnError(start, RazorResources.ParseError_TextTagCannotContainAttributes);

                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                RecoverTextTag();
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Span.CodeGenerator = SpanCodeGenerator.Null;

            CompleteTagBlockWithSpan(tagBlockWrapper, Span.EditHandler.AcceptedCharacters, SpanKind.Transition);

            return seenCloseAngle;
        }

        // Special tags include <! and <? tags
        private bool AtSpecialTag
        {
            get
            {
                return (At(HtmlSymbolType.OpenAngle) &&
                       (NextIs(HtmlSymbolType.Bang) ||
                        NextIs(HtmlSymbolType.QuestionMark)));
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
            if (!At(HtmlSymbolType.WhiteSpace))
            {
                // We should be right after the tag name, so if there's no whitespace, something is wrong
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
            if (At(HtmlSymbolType.Text))
            {
                name = ReadWhile(sym =>
                                 sym.Type != HtmlSymbolType.WhiteSpace &&
                                 sym.Type != HtmlSymbolType.NewLine &&
                                 sym.Type != HtmlSymbolType.Equals &&
                                 sym.Type != HtmlSymbolType.CloseAngle &&
                                 sym.Type != HtmlSymbolType.OpenAngle &&
                                 (sym.Type != HtmlSymbolType.ForwardSlash || !NextIs(HtmlSymbolType.CloseAngle)));
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
                // Saw a space or newline after the name, so just skip this attribute and continue around the loop
                Accept(whitespace);
                Accept(name);
                return;
            }

            Output(SpanKind.Markup);

            // Start a new markup block for the attribute
            using (Context.StartBlock(BlockType.Markup))
            {
                AttributePrefix(whitespace, name);
            }
        }

        private void AttributePrefix(IEnumerable<HtmlSymbol> whitespace, IEnumerable<HtmlSymbol> nameSymbols)
        {
            // First, determine if this is a 'data-' attribute (since those can't use conditional attributes)
            LocationTagged<string> name = nameSymbols.GetContent(Span.Start);
            var attributeCanBeConditional = !name.Value.StartsWith("data-", StringComparison.OrdinalIgnoreCase);

            // Accept the whitespace and name
            Accept(whitespace);
            Accept(nameSymbols);
            Assert(HtmlSymbolType.Equals); // We should be at "="
            AcceptAndMoveNext();
            var quote = HtmlSymbolType.Unknown;
            if (At(HtmlSymbolType.SingleQuote) || At(HtmlSymbolType.DoubleQuote))
            {
                quote = CurrentSymbol.Type;
                AcceptAndMoveNext();
            }

            // We now have the prefix: (i.e. '      foo="')
            LocationTagged<string> prefix = Span.GetContent();

            if (attributeCanBeConditional)
            {
                Span.CodeGenerator = SpanCodeGenerator.Null; // The block code generator will render the prefix
                Output(SpanKind.Markup);

                // Read the values
                while (!EndOfFile && !IsEndOfAttributeValue(quote, CurrentSymbol))
                {
                    AttributeValue(quote);
                }

                // Capture the suffix
                LocationTagged<string> suffix = new LocationTagged<string>(String.Empty, CurrentLocation);
                if (quote != HtmlSymbolType.Unknown && At(quote))
                {
                    suffix = CurrentSymbol.GetContent();
                    AcceptAndMoveNext();
                }

                if (Span.Symbols.Count > 0)
                {
                    Span.CodeGenerator = SpanCodeGenerator.Null; // Again, block code generator will render the suffix
                    Output(SpanKind.Markup);
                }

                // Create the block code generator
                Context.CurrentBlock.CodeGenerator = new AttributeBlockCodeGenerator(
                    name, prefix, suffix);
            }
            else
            {
                // Not a "conditional" attribute, so just read the value
                SkipToAndParseCode(sym => IsEndOfAttributeValue(quote, sym));
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
            Accept(prefix);

            if (At(HtmlSymbolType.Transition))
            {
                var valueStart = CurrentLocation;
                PutCurrentBack();

                // Output the prefix but as a null-span. DynamicAttributeBlockCodeGenerator will render it
                Span.CodeGenerator = SpanCodeGenerator.Null;

                // Dynamic value, start a new block and set the code generator
                using (Context.StartBlock(BlockType.Markup))
                {
                    Context.CurrentBlock.CodeGenerator = 
                        new DynamicAttributeBlockCodeGenerator(prefix.GetContent(prefixStart), valueStart);

                    OtherParserBlock();
                }
            }
            else if (At(HtmlSymbolType.Text) && 
                     CurrentSymbol.Content.Length > 0 && 
                     CurrentSymbol.Content[0] == '~' && 
                     NextIs(HtmlSymbolType.ForwardSlash))
            {
                // Virtual Path value
                var valueStart = CurrentLocation;
                VirtualPath();
                Span.CodeGenerator = new LiteralAttributeCodeGenerator(
                    prefix.GetContent(prefixStart),
                    new LocationTagged<SpanCodeGenerator>(new ResolveUrlCodeGenerator(), valueStart));
            }
            else
            {
                // Literal value
                // 'quote' should be "Unknown" if not quoted and symbols coming from the tokenizer should never have "Unknown" type.
                var value = ReadWhile(sym =>
                                      // These three conditions find separators which break the attribute value into portions
                                      sym.Type != HtmlSymbolType.WhiteSpace &&
                                      sym.Type != HtmlSymbolType.NewLine &&
                                      sym.Type != HtmlSymbolType.Transition &&
                                      // This condition checks for the end of the attribute value (it repeats some of the checks above 
                                      // but for now that's ok)
                                      !IsEndOfAttributeValue(quote, sym));
                Accept(value);
                Span.CodeGenerator = new LiteralAttributeCodeGenerator(prefix.GetContent(prefixStart), value.GetContent(prefixStart));
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

        private void VirtualPath()
        {
            Assert(HtmlSymbolType.Text);
            Debug.Assert(CurrentSymbol.Content.Length > 0 && CurrentSymbol.Content[0] == '~');

            // Parse until a transition symbol, whitespace, newline or quote. We support only a fairly minimal subset of Virtual Paths
            AcceptUntil(HtmlSymbolType.Transition, HtmlSymbolType.WhiteSpace, HtmlSymbolType.NewLine, HtmlSymbolType.SingleQuote, HtmlSymbolType.DoubleQuote);

            // Output a Virtual Path span
            Span.EditHandler.EditorHints = EditorHints.VirtualPath;
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
            // If we're at text, it's the name, otherwise the name is ""
            HtmlSymbol tagName;
            if (At(HtmlSymbolType.Text))
            {
                tagName = CurrentSymbol;
            }
            else
            {
                tagName = new HtmlSymbol(CurrentLocation, String.Empty, HtmlSymbolType.Unknown);
            }

            Tuple<HtmlSymbol, SourceLocation> tag = Tuple.Create(tagName, _lastTagStart);

            if (tags.Count == 0 && String.Equals(tag.Item1.Content, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase))
            {
                Output(SpanKind.Markup);
                Span.CodeGenerator = SpanCodeGenerator.Null;

                Accept(_bufferedOpenAngle);
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
                    Context.OnError(tag.Item2, RazorResources.ParseError_TextTagCannotContainAttributes);

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
                Context.OnError(tag.Item2, RazorResources.FormatParseError_UnfinishedTag(tag.Item1.Content));
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

                                using (Context.StartBlock(BlockType.Tag))
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
                    else if (string.Equals(tagName, "script", StringComparison.OrdinalIgnoreCase))
                    {
                        CompleteTagBlockWithSpan(tagBlockWrapper, AcceptedCharacters.None, SpanKind.Markup);

                        SkipToEndScriptAndParseCode(endTagAcceptedCharacters: AcceptedCharacters.None);
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

                    if (At(HtmlSymbolType.Text) && string.Equals(CurrentSymbol.Content, "script", StringComparison.OrdinalIgnoreCase))
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

                    using (Context.StartBlock(BlockType.Tag))
                    {
                        Span.EditHandler.AcceptedCharacters = endTagAcceptedCharacters;

                        AcceptAndMoveNext(); // '<'
                        AcceptAndMoveNext(); // '/'
                        SkipToAndParseCode(HtmlSymbolType.CloseAngle);
                        if (!Optional(HtmlSymbolType.CloseAngle))
                        {
                            Context.OnError(tagStart, RazorResources.FormatParseError_UnfinishedTag("script"));
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
                if (String.Equals(tagName, currentTag.Item1.Content, StringComparison.OrdinalIgnoreCase))
                {
                    // Matched the tag
                    return true;
                }
            }
            if (currentTag != null)
            {
                Context.OnError(currentTag.Item2, RazorResources.FormatParseError_MissingEndTag(currentTag.Item1.Content));
            }
            else
            {
                Context.OnError(tagStart, RazorResources.FormatParseError_UnexpectedEndTag(tagName));
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
                Tuple<HtmlSymbol, SourceLocation> tag = tags.Pop();
                Context.OnError(tag.Item2, RazorResources.FormatParseError_MissingEndTag(tag.Item1.Content));
            }
            else if (complete)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            tags.Clear();
            if (!Context.DesignTimeMode)
            {
                AcceptWhile(HtmlSymbolType.WhiteSpace);
                if (!EndOfFile && CurrentSymbol.Type == HtmlSymbolType.NewLine)
                {
                    AcceptAndMoveNext();
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
    }
}
