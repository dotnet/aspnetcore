// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>
    {
        internal static readonly int UsingKeywordLength = 5; // using

        internal static ISet<string> DefaultKeywords = new HashSet<string>()
        {
            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
            SyntaxConstants.CSharp.AddTagHelperKeyword,
            SyntaxConstants.CSharp.RemoveTagHelperKeyword,
            "if",
            "do",
            "try",
            "for",
            "foreach",
            "while",
            "switch",
            "lock",
            "using",
            "section",
            "inherits",
            "functions",
            "namespace",
            "class",
        };

        private Dictionary<string, Action> _directiveParsers = new Dictionary<string, Action>();
        private Dictionary<CSharpKeyword, Action<bool>> _keywordParsers = new Dictionary<CSharpKeyword, Action<bool>>();

        public CSharpCodeParser()
        {
            Keywords = new HashSet<string>();
            SetUpKeywords();
            SetupDirectives();
            SetUpExpressions();
        }

        protected internal ISet<string> Keywords { get; private set; }

        public bool IsNested { get; set; }

        protected override ParserBase OtherParser
        {
            get { return Context.MarkupParser; }
        }

        protected override LanguageCharacteristics<CSharpTokenizer, CSharpSymbol, CSharpSymbolType> Language
        {
            get { return CSharpLanguageCharacteristics.Instance; }
        }

        protected void MapDirectives(Action handler, params string[] directives)
        {
            foreach (string directive in directives)
            {
                _directiveParsers.Add(directive, handler);
                Keywords.Add(directive);
            }
        }

        protected bool TryGetDirectiveHandler(string directive, out Action handler)
        {
            return _directiveParsers.TryGetValue(directive, out handler);
        }

        private void MapExpressionKeyword(Action<bool> handler, CSharpKeyword keyword)
        {
            _keywordParsers.Add(keyword, handler);

            // Expression keywords don't belong in the regular keyword list
        }

        private void MapKeywords(Action<bool> handler, params CSharpKeyword[] keywords)
        {
            MapKeywords(handler, topLevel: true, keywords: keywords);
        }

        private void MapKeywords(Action<bool> handler, bool topLevel, params CSharpKeyword[] keywords)
        {
            foreach (CSharpKeyword keyword in keywords)
            {
                _keywordParsers.Add(keyword, handler);
                if (topLevel)
                {
                    Keywords.Add(CSharpLanguageCharacteristics.GetKeyword(keyword));
                }
            }
        }

        [Conditional("DEBUG")]
        internal void Assert(CSharpKeyword expectedKeyword)
        {
            Debug.Assert(CurrentSymbol.Type == CSharpSymbolType.Keyword &&
                CurrentSymbol.Keyword.HasValue &&
                CurrentSymbol.Keyword.Value == expectedKeyword);
        }

        protected internal bool At(CSharpKeyword keyword)
        {
            return At(CSharpSymbolType.Keyword) &&
                CurrentSymbol.Keyword.HasValue &&
                CurrentSymbol.Keyword.Value == keyword;
        }

        protected internal bool AcceptIf(CSharpKeyword keyword)
        {
            if (At(keyword))
            {
                AcceptAndMoveNext();
                return true;
            }
            return false;
        }

        protected static Func<CSharpSymbol, bool> IsSpacingToken(bool includeNewLines, bool includeComments)
        {
            return sym => sym.Type == CSharpSymbolType.WhiteSpace ||
                          (includeNewLines && sym.Type == CSharpSymbolType.NewLine) ||
                          (includeComments && sym.Type == CSharpSymbolType.Comment);
        }

        public override void ParseBlock()
        {
            using (PushSpanConfig(DefaultSpanConfig))
            {
                if (Context == null)
                {
                    throw new InvalidOperationException(RazorResources.Parser_Context_Not_Set);
                }

                // Unless changed, the block is a statement block
                using (Context.StartBlock(BlockType.Statement))
                {
                    NextToken();

                    AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

                    var current = CurrentSymbol;
                    if (At(CSharpSymbolType.StringLiteral) &&
                        CurrentSymbol.Content.Length > 0 &&
                        CurrentSymbol.Content[0] == SyntaxConstants.TransitionCharacter)
                    {
                        var split = Language.SplitSymbol(CurrentSymbol, 1, CSharpSymbolType.Transition);
                        current = split.Item1;
                        Context.Source.Position = split.Item2.Start.AbsoluteIndex;
                        NextToken();
                    }
                    else if (At(CSharpSymbolType.Transition))
                    {
                        NextToken();
                    }

                    // Accept "@" if we see it, but if we don't, that's OK. We assume we were started for a good reason
                    if (current.Type == CSharpSymbolType.Transition)
                    {
                        if (Span.Symbols.Count > 0)
                        {
                            Output(SpanKind.Code);
                        }
                        AtTransition(current);
                    }
                    else
                    {
                        // No "@" => Jump straight to AfterTransition
                        AfterTransition();
                    }
                    Output(SpanKind.Code);
                }
            }
        }

        private void DefaultSpanConfig(SpanBuilder span)
        {
            span.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
            span.ChunkGenerator = new StatementChunkGenerator();
        }

        private void AtTransition(CSharpSymbol current)
        {
            Debug.Assert(current.Type == CSharpSymbolType.Transition);
            Accept(current);
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;

            // Output the "@" span and continue here
            Output(SpanKind.Transition);
            AfterTransition();
        }

        private void AfterTransition()
        {
            using (PushSpanConfig(DefaultSpanConfig))
            {
                EnsureCurrent();
                try
                {
                    // What type of block is this?
                    if (!EndOfFile)
                    {
                        if (CurrentSymbol.Type == CSharpSymbolType.LeftParenthesis)
                        {
                            Context.CurrentBlock.Type = BlockType.Expression;
                            Context.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                            ExplicitExpression();
                            return;
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.Identifier)
                        {
                            Action handler;
                            if (TryGetDirectiveHandler(CurrentSymbol.Content, out handler))
                            {
                                Span.ChunkGenerator = SpanChunkGenerator.Null;
                                handler();
                                return;
                            }
                            else
                            {
                                if (string.Equals(
                                    CurrentSymbol.Content,
                                    SyntaxConstants.CSharp.HelperKeyword,
                                    StringComparison.Ordinal))
                                {
                                    Context.OnError(
                                        CurrentLocation,
                                        RazorResources.FormatParseError_HelperDirectiveNotAvailable(
                                            SyntaxConstants.CSharp.HelperKeyword),
                                        CurrentSymbol.Content.Length);
                                }

                                Context.CurrentBlock.Type = BlockType.Expression;
                                Context.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                                ImplicitExpression();
                                return;
                            }
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.Keyword)
                        {
                            KeywordBlock(topLevel: true);
                            return;
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.LeftBrace)
                        {
                            VerbatimBlock();
                            return;
                        }
                    }

                    // Invalid character
                    Context.CurrentBlock.Type = BlockType.Expression;
                    Context.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                    AddMarkerSymbolIfNecessary();
                    Span.ChunkGenerator = new ExpressionChunkGenerator();
                    Span.EditHandler = new ImplicitExpressionEditHandler(
                        Language.TokenizeString,
                        DefaultKeywords,
                        acceptTrailingDot: IsNested)
                    {
                        AcceptedCharacters = AcceptedCharacters.NonWhiteSpace
                    };
                    if (At(CSharpSymbolType.WhiteSpace) || At(CSharpSymbolType.NewLine))
                    {
                        Context.OnError(
                            CurrentLocation,
                            RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS,
                            CurrentSymbol.Content.Length);
                    }
                    else if (EndOfFile)
                    {
                        Context.OnError(
                            CurrentLocation,
                            RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                            length: 1 /* end of file */);
                    }
                    else
                    {
                        Context.OnError(
                            CurrentLocation,
                            RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS(
                                CurrentSymbol.Content),
                            CurrentSymbol.Content.Length);
                    }
                }
                finally
                {
                    // Always put current character back in the buffer for the next parser.
                    PutCurrentBack();
                }
            }
        }

        private void VerbatimBlock()
        {
            Assert(CSharpSymbolType.LeftBrace);
            var block = new Block(RazorResources.BlockName_Code, CurrentLocation);
            AcceptAndMoveNext();

            // Set up the "{" span and output
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Output(SpanKind.MetaCode);

            // Set up auto-complete and parse the code block
            var editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;
            CodeBlock(false, block);

            Span.ChunkGenerator = new StatementChunkGenerator();
            AddMarkerSymbolIfNecessary();
            if (!At(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
            }
            Output(SpanKind.Code);

            if (Optional(CSharpSymbolType.RightBrace))
            {
                // Set up the "}" span
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                Span.ChunkGenerator = SpanChunkGenerator.Null;
            }

            if (!IsNested)
            {
                EnsureCurrent();
                if (At(CSharpSymbolType.NewLine) ||
                    (At(CSharpSymbolType.WhiteSpace) && NextIs(CSharpSymbolType.NewLine)))
                {
                    Context.NullGenerateWhitespaceAndNewLine = true;
                }
            }

            Output(SpanKind.MetaCode);
        }

        private void ImplicitExpression()
        {
            ImplicitExpression(AcceptedCharacters.NonWhiteSpace);
        }

        // Async implicit expressions include the "await" keyword and therefore need to allow spaces to
        // separate the "await" and the following code.
        private void AsyncImplicitExpression()
        {
            ImplicitExpression(AcceptedCharacters.AnyExceptNewline);
        }

        private void ImplicitExpression(AcceptedCharacters acceptedCharacters)
        {
            Context.CurrentBlock.Type = BlockType.Expression;
            Context.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();

            using (PushSpanConfig(span =>
            {
                span.EditHandler = new ImplicitExpressionEditHandler(
                    Language.TokenizeString,
                    Keywords,
                    acceptTrailingDot: IsNested);
                span.EditHandler.AcceptedCharacters = acceptedCharacters;
                span.ChunkGenerator = new ExpressionChunkGenerator();
            }))
            {
                do
                {
                    if (AtIdentifier(allowKeywords: true))
                    {
                        AcceptAndMoveNext();
                    }
                }
                while (MethodCallOrArrayIndex(acceptedCharacters));

                PutCurrentBack();
                Output(SpanKind.Code);
            }
        }

        private bool MethodCallOrArrayIndex(AcceptedCharacters acceptedCharacters)
        {
            if (!EndOfFile)
            {
                if (CurrentSymbol.Type == CSharpSymbolType.LeftParenthesis ||
                    CurrentSymbol.Type == CSharpSymbolType.LeftBracket)
                {
                    // If we end within "(", whitespace is fine
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;

                    CSharpSymbolType right;
                    bool success;

                    using (PushSpanConfig((span, prev) =>
                    {
                        prev(span);
                        span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                    }))
                    {
                        right = Language.FlipBracket(CurrentSymbol.Type);
                        success = Balance(BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                    }

                    if (!success)
                    {
                        AcceptUntil(CSharpSymbolType.LessThan);
                    }
                    if (At(right))
                    {
                        AcceptAndMoveNext();

                        // At the ending brace, restore the initial accepted characters.
                        Span.EditHandler.AcceptedCharacters = acceptedCharacters;
                    }
                    return MethodCallOrArrayIndex(acceptedCharacters);
                }
                if (At(CSharpSymbolType.QuestionMark))
                {
                    var next = Lookahead(count: 1);

                    if (next != null)
                    {
                        if (next.Type == CSharpSymbolType.Dot)
                        {
                            // Accept null conditional dot operator (?.).
                            AcceptAndMoveNext();
                            AcceptAndMoveNext();

                            // If the next piece after the ?. is a keyword or identifier then we want to continue.
                            return At(CSharpSymbolType.Identifier) || At(CSharpSymbolType.Keyword);
                        }
                        else if (next.Type == CSharpSymbolType.LeftBracket)
                        {
                            // We're at the ? for a null conditional bracket operator (?[).
                            AcceptAndMoveNext();

                            // Accept the [ and any content inside (it will attempt to balance).
                            return MethodCallOrArrayIndex(acceptedCharacters);
                        }
                    }
                }
                else if (At(CSharpSymbolType.Dot))
                {
                    var dot = CurrentSymbol;
                    if (NextToken())
                    {
                        if (At(CSharpSymbolType.Identifier) || At(CSharpSymbolType.Keyword))
                        {
                            // Accept the dot and return to the start
                            Accept(dot);
                            return true; // continue
                        }
                        else
                        {
                            // Put the symbol back
                            PutCurrentBack();
                        }
                    }
                    if (!IsNested)
                    {
                        // Put the "." back
                        PutBack(dot);
                    }
                    else
                    {
                        Accept(dot);
                    }
                }
                else if (!At(CSharpSymbolType.WhiteSpace) && !At(CSharpSymbolType.NewLine))
                {
                    PutCurrentBack();
                }
            }

            // Implicit Expression is complete
            return false;
        }

        protected void CompleteBlock()
        {
            CompleteBlock(insertMarkerIfNecessary: true);
        }

        protected void CompleteBlock(bool insertMarkerIfNecessary)
        {
            CompleteBlock(insertMarkerIfNecessary, captureWhitespaceToEndOfLine: insertMarkerIfNecessary);
        }

        protected void CompleteBlock(bool insertMarkerIfNecessary, bool captureWhitespaceToEndOfLine)
        {
            if (insertMarkerIfNecessary && Context.LastAcceptedCharacters != AcceptedCharacters.Any)
            {
                AddMarkerSymbolIfNecessary();
            }

            EnsureCurrent();

            // Read whitespace, but not newlines
            // If we're not inserting a marker span, we don't need to capture whitespace
            if (!Context.WhiteSpaceIsSignificantToAncestorBlock &&
                Context.CurrentBlock.Type != BlockType.Expression &&
                captureWhitespaceToEndOfLine &&
                !Context.DesignTimeMode &&
                !IsNested)
            {
                CaptureWhitespaceAtEndOfCodeOnlyLine();
            }
            else
            {
                PutCurrentBack();
            }
        }

        private void CaptureWhitespaceAtEndOfCodeOnlyLine()
        {
            IEnumerable<CSharpSymbol> ws = ReadWhile(sym => sym.Type == CSharpSymbolType.WhiteSpace);
            if (At(CSharpSymbolType.NewLine))
            {
                Accept(ws);
                AcceptAndMoveNext();
                PutCurrentBack();
            }
            else
            {
                PutCurrentBack();
                PutBack(ws);
            }
        }

        private void ConfigureExplicitExpressionSpan(SpanBuilder sb)
        {
            sb.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
            sb.ChunkGenerator = new ExpressionChunkGenerator();
        }

        private void ExplicitExpression()
        {
            var block = new Block(RazorResources.BlockName_ExplicitExpression, CurrentLocation);
            Assert(CSharpSymbolType.LeftParenthesis);
            AcceptAndMoveNext();
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Output(SpanKind.MetaCode);
            using (PushSpanConfig(ConfigureExplicitExpressionSpan))
            {
                var success = Balance(
                    BalancingModes.BacktrackOnFailure |
                        BalancingModes.NoErrorOnFailure |
                        BalancingModes.AllowCommentsAndTemplates,
                    CSharpSymbolType.LeftParenthesis,
                    CSharpSymbolType.RightParenthesis,
                    block.Start);

                if (!success)
                {
                    AcceptUntil(CSharpSymbolType.LessThan);
                    Context.OnError(
                        block.Start,
                        RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(block.Name, ")", "("),
                        length: 1 /* ( */);
                }

                // If necessary, put an empty-content marker symbol here
                if (Span.Symbols.Count == 0)
                {
                    Accept(new CSharpSymbol(CurrentLocation, string.Empty, CSharpSymbolType.Unknown));
                }

                // Output the content span and then capture the ")"
                Output(SpanKind.Code);
            }
            Optional(CSharpSymbolType.RightParenthesis);
            if (!EndOfFile)
            {
                PutCurrentBack();
            }
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            CompleteBlock(insertMarkerIfNecessary: false);
            Output(SpanKind.MetaCode);
        }

        private void Template()
        {
            if (Context.IsWithin(BlockType.Template))
            {
                Context.OnError(
                    CurrentLocation,
                    RazorResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
                    length: 1 /* @ */);
            }
            Output(SpanKind.Code);
            using (Context.StartBlock(BlockType.Template))
            {
                Context.CurrentBlock.ChunkGenerator = new TemplateBlockChunkGenerator();
                PutCurrentBack();
                OtherParserBlock();
            }
        }

        private void OtherParserBlock()
        {
            ParseWithOtherParser(p => p.ParseBlock());
        }

        private void SectionBlock(string left, string right, bool caseSensitive)
        {
            ParseWithOtherParser(p => p.ParseSection(Tuple.Create(left, right), caseSensitive));
        }

        private void NestedBlock()
        {
            Output(SpanKind.Code);
            var wasNested = IsNested;
            IsNested = true;
            using (PushSpanConfig())
            {
                ParseBlock();
            }
            Initialize(Span);
            IsNested = wasNested;
            NextToken();
        }

        protected override bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
        {
            // No embedded transitions in C#, so ignore that param
            return allowTemplatesAndComments
                   && ((Language.IsTransition(CurrentSymbol)
                        && NextIs(CSharpSymbolType.LessThan, CSharpSymbolType.Colon))
                       || Language.IsCommentStart(CurrentSymbol));
        }

        protected override void HandleEmbeddedTransition()
        {
            if (Language.IsTransition(CurrentSymbol))
            {
                PutCurrentBack();
                Template();
            }
            else if (Language.IsCommentStart(CurrentSymbol))
            {
                RazorComment();
            }
        }

        private void ParseWithOtherParser(Action<ParserBase> parseAction)
        {
            // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
            // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
            // as C#; it should be handled as a period because it's wrapped in markup.
            var wasNested = IsNested;
            IsNested = false;
            using (PushSpanConfig())
            {
                Context.SwitchActiveParser();
                parseAction(Context.MarkupParser);
                Context.SwitchActiveParser();
            }
            Initialize(Span);
            IsNested = wasNested;
            NextToken();
        }
    }
}
