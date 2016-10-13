// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>
    {
        internal static readonly int UsingKeywordLength = 5; // using
        private static readonly Func<CSharpSymbol, bool> IsValidStatementSpacingSymbol =
            IsSpacingToken(includeNewLines: true, includeComments: true);

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

        private Dictionary<string, Action> _directiveParsers = new Dictionary<string, Action>(StringComparer.Ordinal);
        private Dictionary<CSharpKeyword, Action<bool>> _keywordParsers = new Dictionary<CSharpKeyword, Action<bool>>();

        public CSharpCodeParser(ParserContext context)
            : base(CSharpLanguageCharacteristics.Instance, context)
        {
            Keywords = new HashSet<string>();
            SetUpKeywords();
            SetupDirectives();
            SetUpExpressions();
        }

        public HtmlMarkupParser HtmlParser { get; set; }

        protected internal ISet<string> Keywords { get; private set; }

        public bool IsNested { get; set; }

        protected override bool SymbolTypeEquals(CSharpSymbolType x, CSharpSymbolType y) => x == y;

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
                    throw new InvalidOperationException(LegacyResources.Parser_Context_Not_Set);
                }

                // Unless changed, the block is a statement block
                using (Context.Builder.StartBlock(BlockType.Statement))
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
                            Context.Builder.CurrentBlock.Type = BlockType.Expression;
                            Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
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
                                    Context.ErrorSink.OnError(
                                        CurrentLocation,
                                        LegacyResources.FormatParseError_HelperDirectiveNotAvailable(
                                            SyntaxConstants.CSharp.HelperKeyword),
                                        CurrentSymbol.Content.Length);
                                }

                                Context.Builder.CurrentBlock.Type = BlockType.Expression;
                                Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
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
                    Context.Builder.CurrentBlock.Type = BlockType.Expression;
                    Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
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
                        Context.ErrorSink.OnError(
                            CurrentLocation,
                            LegacyResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS,
                            CurrentSymbol.Content.Length);
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            CurrentLocation,
                            LegacyResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                            length: 1 /* end of file */);
                    }
                    else
                    {
                        Context.ErrorSink.OnError(
                            CurrentLocation,
                            LegacyResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS(
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
            var block = new Block(LegacyResources.BlockName_Code, CurrentLocation);
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
            Context.Builder.CurrentBlock.Type = BlockType.Expression;
            Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();

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
            if (insertMarkerIfNecessary && Context.Builder.LastAcceptedCharacters != AcceptedCharacters.Any)
            {
                AddMarkerSymbolIfNecessary();
            }

            EnsureCurrent();

            // Read whitespace, but not newlines
            // If we're not inserting a marker span, we don't need to capture whitespace
            if (!Context.WhiteSpaceIsSignificantToAncestorBlock &&
                Context.Builder.CurrentBlock.Type != BlockType.Expression &&
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
            var block = new Block(LegacyResources.BlockName_ExplicitExpression, CurrentLocation);
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
                    Context.ErrorSink.OnError(
                        block.Start,
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(block.Name, ")", "("),
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
            if (Context.Builder.ActiveBlocks.Any(block => block.Type == BlockType.Template))
            {
                Context.ErrorSink.OnError(
                    CurrentLocation,
                    LegacyResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
                    length: 1 /* @ */);
            }
            Output(SpanKind.Code);
            using (Context.Builder.StartBlock(BlockType.Template))
            {
                Context.Builder.CurrentBlock.ChunkGenerator = new TemplateBlockChunkGenerator();
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
                        && NextIs(CSharpSymbolType.LessThan, CSharpSymbolType.Colon, CSharpSymbolType.DoubleColon))
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

        private void ParseWithOtherParser(Action<HtmlMarkupParser> parseAction)
        {
            // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
            // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
            // as C#; it should be handled as a period because it's wrapped in markup.
            var wasNested = IsNested;
            IsNested = false;
            using (PushSpanConfig())
            {
                parseAction(HtmlParser);
            }
            Initialize(Span);
            IsNested = wasNested;
            NextToken();
        }

        private void SetUpKeywords()
        {
            MapKeywords(
                ConditionalBlock,
                CSharpKeyword.For,
                CSharpKeyword.Foreach,
                CSharpKeyword.While,
                CSharpKeyword.Switch,
                CSharpKeyword.Lock);
            MapKeywords(CaseStatement, false, CSharpKeyword.Case, CSharpKeyword.Default);
            MapKeywords(IfStatement, CSharpKeyword.If);
            MapKeywords(TryStatement, CSharpKeyword.Try);
            MapKeywords(UsingKeyword, CSharpKeyword.Using);
            MapKeywords(DoStatement, CSharpKeyword.Do);
            MapKeywords(ReservedDirective, CSharpKeyword.Namespace, CSharpKeyword.Class);
        }

        protected virtual void ReservedDirective(bool topLevel)
        {
            Context.ErrorSink.OnError(
                CurrentLocation,
                LegacyResources.FormatParseError_ReservedWord(CurrentSymbol.Content),
                CurrentSymbol.Content.Length);
            AcceptAndMoveNext();
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Context.Builder.CurrentBlock.Type = BlockType.Directive;
            CompleteBlock();
            Output(SpanKind.MetaCode);
        }

        private void KeywordBlock(bool topLevel)
        {
            HandleKeyword(topLevel, () =>
            {
                Context.Builder.CurrentBlock.Type = BlockType.Expression;
                Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                ImplicitExpression();
            });
        }

        private void CaseStatement(bool topLevel)
        {
            Assert(CSharpSymbolType.Keyword);
            Debug.Assert(CurrentSymbol.Keyword != null &&
                         (CurrentSymbol.Keyword.Value == CSharpKeyword.Case ||
                          CurrentSymbol.Keyword.Value == CSharpKeyword.Default));
            AcceptUntil(CSharpSymbolType.Colon);
            Optional(CSharpSymbolType.Colon);
        }

        private void DoStatement(bool topLevel)
        {
            Assert(CSharpKeyword.Do);
            UnconditionalBlock();
            WhileClause();
            if (topLevel)
            {
                CompleteBlock();
            }
        }

        private void WhileClause()
        {
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
            IEnumerable<CSharpSymbol> ws = SkipToNextImportantToken();

            if (At(CSharpKeyword.While))
            {
                Accept(ws);
                Assert(CSharpKeyword.While);
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (AcceptCondition() && Optional(CSharpSymbolType.Semicolon))
                {
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                }
            }
            else
            {
                PutCurrentBack();
                PutBack(ws);
            }
        }

        private void UsingKeyword(bool topLevel)
        {
            Assert(CSharpKeyword.Using);
            var block = new Block(CurrentSymbol);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (At(CSharpSymbolType.LeftParenthesis))
            {
                // using ( ==> Using Statement
                UsingStatement(block);
            }
            else if (At(CSharpSymbolType.Identifier) || At(CSharpKeyword.Static))
            {
                // using Identifier ==> Using Declaration
                if (!topLevel)
                {
                    Context.ErrorSink.OnError(
                        block.Start,
                        LegacyResources.ParseError_NamespaceImportAndTypeAlias_Cannot_Exist_Within_CodeBlock,
                        block.Name.Length);
                    StandardStatement();
                }
                else
                {
                    UsingDeclaration();
                }
            }

            if (topLevel)
            {
                CompleteBlock();
            }
        }

        private void UsingDeclaration()
        {
            // Set block type to directive
            Context.Builder.CurrentBlock.Type = BlockType.Directive;

            if (At(CSharpSymbolType.Identifier))
            {
                // non-static using
                NamespaceOrTypeName();
                var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (At(CSharpSymbolType.Assign))
                {
                    // Alias
                    Accept(whitespace);
                    Assert(CSharpSymbolType.Assign);
                    AcceptAndMoveNext();

                    AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

                    // One more namespace or type name
                    NamespaceOrTypeName();
                }
                else
                {
                    PutCurrentBack();
                    PutBack(whitespace);
                }
            }
            else if (At(CSharpKeyword.Static))
            {
                // static using
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));
                NamespaceOrTypeName();
            }

            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.AnyExceptNewline;
            Span.ChunkGenerator = new AddImportChunkGenerator(
                Span.GetContent(symbols => symbols.Skip(1)));

            // Optional ";"
            if (EnsureCurrent())
            {
                Optional(CSharpSymbolType.Semicolon);
            }
        }

        protected bool NamespaceOrTypeName()
        {
            if (Optional(CSharpSymbolType.Identifier) || Optional(CSharpSymbolType.Keyword))
            {
                Optional(CSharpSymbolType.QuestionMark); // Nullable
                if (Optional(CSharpSymbolType.DoubleColon))
                {
                    if (!Optional(CSharpSymbolType.Identifier))
                    {
                        Optional(CSharpSymbolType.Keyword);
                    }
                }
                if (At(CSharpSymbolType.LessThan))
                {
                    TypeArgumentList();
                }
                if (Optional(CSharpSymbolType.Dot))
                {
                    NamespaceOrTypeName();
                }
                while (At(CSharpSymbolType.LeftBracket))
                {
                    Balance(BalancingModes.None);
                    Optional(CSharpSymbolType.RightBracket);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TypeArgumentList()
        {
            Assert(CSharpSymbolType.LessThan);
            Balance(BalancingModes.None);
            Optional(CSharpSymbolType.GreaterThan);
        }

        private void UsingStatement(Block block)
        {
            Assert(CSharpSymbolType.LeftParenthesis);

            // Parse condition
            if (AcceptCondition())
            {
                AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

                // Parse code block
                ExpectCodeBlock(block);
            }
        }

        private void TryStatement(bool topLevel)
        {
            Assert(CSharpKeyword.Try);
            UnconditionalBlock();
            AfterTryClause();
            if (topLevel)
            {
                CompleteBlock();
            }
        }

        private void IfStatement(bool topLevel)
        {
            Assert(CSharpKeyword.If);
            ConditionalBlock(topLevel: false);
            AfterIfClause();
            if (topLevel)
            {
                CompleteBlock();
            }
        }

        private void AfterTryClause()
        {
            // Grab whitespace
            var whitespace = SkipToNextImportantToken();

            // Check for a catch or finally part
            if (At(CSharpKeyword.Catch))
            {
                Accept(whitespace);
                Assert(CSharpKeyword.Catch);
                FilterableCatchBlock();
                AfterTryClause();
            }
            else if (At(CSharpKeyword.Finally))
            {
                Accept(whitespace);
                Assert(CSharpKeyword.Finally);
                UnconditionalBlock();
            }
            else
            {
                // Return whitespace and end the block
                PutCurrentBack();
                PutBack(whitespace);
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
            }
        }

        private void AfterIfClause()
        {
            // Grab whitespace and razor comments
            IEnumerable<CSharpSymbol> ws = SkipToNextImportantToken();

            // Check for an else part
            if (At(CSharpKeyword.Else))
            {
                Accept(ws);
                Assert(CSharpKeyword.Else);
                ElseClause();
            }
            else
            {
                // No else, return whitespace
                PutCurrentBack();
                PutBack(ws);
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
            }
        }

        private void ElseClause()
        {
            if (!At(CSharpKeyword.Else))
            {
                return;
            }
            var block = new Block(CurrentSymbol);

            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
            if (At(CSharpKeyword.If))
            {
                // ElseIf
                block.Name = SyntaxConstants.CSharp.ElseIfKeyword;
                ConditionalBlock(block);
                AfterIfClause();
            }
            else if (!EndOfFile)
            {
                // Else
                ExpectCodeBlock(block);
            }
        }

        private void ExpectCodeBlock(Block block)
        {
            if (!EndOfFile)
            {
                // Check for "{" to make sure we're at a block
                if (!At(CSharpSymbolType.LeftBrace))
                {
                    Context.ErrorSink.OnError(
                        CurrentLocation,
                        LegacyResources.FormatParseError_SingleLine_ControlFlowStatements_Not_Allowed(
                            Language.GetSample(CSharpSymbolType.LeftBrace),
                            CurrentSymbol.Content),
                        CurrentSymbol.Content.Length);
                }

                // Parse the statement and then we're done
                Statement(block);
            }
        }

        private void UnconditionalBlock()
        {
            Assert(CSharpSymbolType.Keyword);
            var block = new Block(CurrentSymbol);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
            ExpectCodeBlock(block);
        }

        private void FilterableCatchBlock()
        {
            Assert(CSharpKeyword.Catch);

            var block = new Block(CurrentSymbol);

            // Accept "catch"
            AcceptAndMoveNext();
            AcceptWhile(IsValidStatementSpacingSymbol);

            // Parse the catch condition if present. If not present, let the C# compiler complain.
            if (AcceptCondition())
            {
                AcceptWhile(IsValidStatementSpacingSymbol);

                if (At(CSharpKeyword.When))
                {
                    // Accept "when".
                    AcceptAndMoveNext();
                    AcceptWhile(IsValidStatementSpacingSymbol);

                    // Parse the filter condition if present. If not present, let the C# compiler complain.
                    if (!AcceptCondition())
                    {
                        // Incomplete condition.
                        return;
                    }

                    AcceptWhile(IsValidStatementSpacingSymbol);
                }

                ExpectCodeBlock(block);
            }
        }

        private void ConditionalBlock(bool topLevel)
        {
            Assert(CSharpSymbolType.Keyword);
            var block = new Block(CurrentSymbol);
            ConditionalBlock(block);
            if (topLevel)
            {
                CompleteBlock();
            }
        }

        private void ConditionalBlock(Block block)
        {
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

            // Parse the condition, if present (if not present, we'll let the C# compiler complain)
            if (AcceptCondition())
            {
                AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                ExpectCodeBlock(block);
            }
        }

        private bool AcceptCondition()
        {
            if (At(CSharpSymbolType.LeftParenthesis))
            {
                var complete = Balance(BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                if (!complete)
                {
                    AcceptUntil(CSharpSymbolType.NewLine);
                }
                else
                {
                    Optional(CSharpSymbolType.RightParenthesis);
                }
                return complete;
            }
            return true;
        }

        private void Statement()
        {
            Statement(null);
        }

        private void Statement(Block block)
        {
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;

            // Accept whitespace but always keep the last whitespace node so we can put it back if necessary
            var lastWhitespace = AcceptWhiteSpaceInLines();
            Debug.Assert(lastWhitespace == null ||
                (lastWhitespace.Start.AbsoluteIndex + lastWhitespace.Content.Length == CurrentLocation.AbsoluteIndex));

            if (EndOfFile)
            {
                if (lastWhitespace != null)
                {
                    Accept(lastWhitespace);
                }
                return;
            }

            var type = CurrentSymbol.Type;
            var loc = CurrentLocation;

            // Both cases @: and @:: are triggered as markup, second colon in second case will be triggered as a plain text
            var isSingleLineMarkup = type == CSharpSymbolType.Transition &&
                (NextIs(CSharpSymbolType.Colon, CSharpSymbolType.DoubleColon));

            var isMarkup = isSingleLineMarkup ||
                type == CSharpSymbolType.LessThan ||
                (type == CSharpSymbolType.Transition && NextIs(CSharpSymbolType.LessThan));

            if (Context.DesignTimeMode || !isMarkup)
            {
                // CODE owns whitespace, MARKUP owns it ONLY in DesignTimeMode.
                if (lastWhitespace != null)
                {
                    Accept(lastWhitespace);
                }
            }
            else
            {
                var nextSymbol = Lookahead(1);

                // MARKUP owns whitespace EXCEPT in DesignTimeMode.
                PutCurrentBack();

                // Put back the whitespace unless it precedes a '<text>' tag.
                if (nextSymbol != null &&
                    !string.Equals(nextSymbol.Content, SyntaxConstants.TextTagName, StringComparison.Ordinal))
                {
                    PutBack(lastWhitespace);
                }
            }

            if (isMarkup)
            {
                if (type == CSharpSymbolType.Transition && !isSingleLineMarkup)
                {
                    Context.ErrorSink.OnError(
                        loc,
                        LegacyResources.ParseError_AtInCode_Must_Be_Followed_By_Colon_Paren_Or_Identifier_Start,
                        length: 1 /* @ */);
                }

                // Markup block
                Output(SpanKind.Code);
                if (Context.DesignTimeMode && CurrentSymbol != null &&
                    (CurrentSymbol.Type == CSharpSymbolType.LessThan || CurrentSymbol.Type == CSharpSymbolType.Transition))
                {
                    PutCurrentBack();
                }
                OtherParserBlock();
            }
            else
            {
                // What kind of statement is this?
                HandleStatement(block, type);
            }
        }

        private void HandleStatement(Block block, CSharpSymbolType type)
        {
            switch (type)
            {
                case CSharpSymbolType.RazorCommentTransition:
                    Output(SpanKind.Code);
                    RazorComment();
                    Statement(block);
                    break;
                case CSharpSymbolType.LeftBrace:
                    // Verbatim Block
                    block = block ?? new Block(LegacyResources.BlockName_Code, CurrentLocation);
                    AcceptAndMoveNext();
                    CodeBlock(block);
                    break;
                case CSharpSymbolType.Keyword:
                    // Keyword block
                    HandleKeyword(false, StandardStatement);
                    break;
                case CSharpSymbolType.Transition:
                    // Embedded Expression block
                    EmbeddedExpression();
                    break;
                case CSharpSymbolType.RightBrace:
                    // Possible end of Code Block, just run the continuation
                    break;
                case CSharpSymbolType.Comment:
                    AcceptAndMoveNext();
                    break;
                default:
                    // Other statement
                    StandardStatement();
                    break;
            }
        }

        private void EmbeddedExpression()
        {
            // First, verify the type of the block
            Assert(CSharpSymbolType.Transition);
            var transition = CurrentSymbol;
            NextToken();

            if (At(CSharpSymbolType.Transition))
            {
                // Escaped "@"
                Output(SpanKind.Code);

                // Output "@" as hidden span
                Accept(transition);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKind.Code);

                Assert(CSharpSymbolType.Transition);
                AcceptAndMoveNext();
                StandardStatement();
            }
            else
            {
                // Throw errors as necessary, but continue parsing
                if (At(CSharpSymbolType.LeftBrace))
                {
                    Context.ErrorSink.OnError(
                        CurrentLocation,
                        LegacyResources.ParseError_Unexpected_Nested_CodeBlock,
                        length: 1  /* { */);
                }

                // @( or @foo - Nested expression, parse a child block
                PutCurrentBack();
                PutBack(transition);

                // Before exiting, add a marker span if necessary
                AddMarkerSymbolIfNecessary();

                NestedBlock();
            }
        }

        private void StandardStatement()
        {
            while (!EndOfFile)
            {
                var bookmark = CurrentLocation.AbsoluteIndex;
                IEnumerable<CSharpSymbol> read = ReadWhile(sym => sym.Type != CSharpSymbolType.Semicolon &&
                                                                  sym.Type != CSharpSymbolType.RazorCommentTransition &&
                                                                  sym.Type != CSharpSymbolType.Transition &&
                                                                  sym.Type != CSharpSymbolType.LeftBrace &&
                                                                  sym.Type != CSharpSymbolType.LeftParenthesis &&
                                                                  sym.Type != CSharpSymbolType.LeftBracket &&
                                                                  sym.Type != CSharpSymbolType.RightBrace);
                if (At(CSharpSymbolType.LeftBrace) ||
                    At(CSharpSymbolType.LeftParenthesis) ||
                    At(CSharpSymbolType.LeftBracket))
                {
                    Accept(read);
                    if (Balance(BalancingModes.AllowCommentsAndTemplates | BalancingModes.BacktrackOnFailure))
                    {
                        Optional(CSharpSymbolType.RightBrace);
                    }
                    else
                    {
                        // Recovery
                        AcceptUntil(CSharpSymbolType.LessThan, CSharpSymbolType.RightBrace);
                        return;
                    }
                }
                else if (At(CSharpSymbolType.Transition) && (NextIs(CSharpSymbolType.LessThan, CSharpSymbolType.Colon)))
                {
                    Accept(read);
                    Output(SpanKind.Code);
                    Template();
                }
                else if (At(CSharpSymbolType.RazorCommentTransition))
                {
                    Accept(read);
                    RazorComment();
                }
                else if (At(CSharpSymbolType.Semicolon))
                {
                    Accept(read);
                    AcceptAndMoveNext();
                    return;
                }
                else if (At(CSharpSymbolType.RightBrace))
                {
                    Accept(read);
                    return;
                }
                else
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                    AcceptUntil(CSharpSymbolType.LessThan, CSharpSymbolType.LeftBrace, CSharpSymbolType.RightBrace);
                    return;
                }
            }
        }

        private void CodeBlock(Block block)
        {
            CodeBlock(true, block);
        }

        private void CodeBlock(bool acceptTerminatingBrace, Block block)
        {
            EnsureCurrent();
            while (!EndOfFile && !At(CSharpSymbolType.RightBrace))
            {
                // Parse a statement, then return here
                Statement();
                EnsureCurrent();
            }

            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    block.Start,
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(block.Name, '}', '{'),
                    length: 1  /* { OR } */);
            }
            else if (acceptTerminatingBrace)
            {
                Assert(CSharpSymbolType.RightBrace);
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                AcceptAndMoveNext();
            }
        }

        private void HandleKeyword(bool topLevel, Action fallback)
        {
            Debug.Assert(CurrentSymbol.Type == CSharpSymbolType.Keyword && CurrentSymbol.Keyword != null);
            Action<bool> handler;
            if (_keywordParsers.TryGetValue(CurrentSymbol.Keyword.Value, out handler))
            {
                handler(topLevel);
            }
            else
            {
                fallback();
            }
        }

        private IEnumerable<CSharpSymbol> SkipToNextImportantToken()
        {
            while (!EndOfFile)
            {
                IEnumerable<CSharpSymbol> ws = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (At(CSharpSymbolType.RazorCommentTransition))
                {
                    Accept(ws);
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                    RazorComment();
                }
                else
                {
                    return ws;
                }
            }
            return Enumerable.Empty<CSharpSymbol>();
        }

        // Common code for Parsers, but FxCop REALLY doesn't like it in the base class.. moving it here for now.
        protected override void OutputSpanBeforeRazorComment()
        {
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.Code);
        }

        private void SetUpExpressions()
        {
            MapExpressionKeyword(AwaitExpression, CSharpKeyword.Await);
        }

        private void AwaitExpression(bool topLevel)
        {
            // Ensure that we're on the await statement (only runs in debug)
            Assert(CSharpKeyword.Await);

            // Accept the "await" and move on
            AcceptAndMoveNext();

            // Accept 1 or more spaces between the await and the following code.
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            // Top level basically indicates if we're within an expression or statement.
            // Ex: topLevel true = @await Foo()  |  topLevel false = @{ await Foo(); }
            // Note that in this case @{ <b>@await Foo()</b> } top level is true for await.
            // Therefore, if we're top level then we want to act like an implicit expression,
            // otherwise just act as whatever we're contained in.
            if (topLevel)
            {
                // Setup the Span to be an async implicit expression (an implicit expresison that allows spaces).
                // Spaces are allowed because of "@await Foo()".
                AsyncImplicitExpression();
            }
        }

        private void SetupDirectives()
        {
            MapDirectives(TagHelperPrefixDirective, SyntaxConstants.CSharp.TagHelperPrefixKeyword);
            MapDirectives(AddTagHelperDirective, SyntaxConstants.CSharp.AddTagHelperKeyword);
            MapDirectives(RemoveTagHelperDirective, SyntaxConstants.CSharp.RemoveTagHelperKeyword);
            MapDirectives(InheritsDirective, SyntaxConstants.CSharp.InheritsKeyword);
            MapDirectives(FunctionsDirective, SyntaxConstants.CSharp.FunctionsKeyword);
            MapDirectives(SectionDirective, SyntaxConstants.CSharp.SectionKeyword);
        }

        protected virtual void TagHelperPrefixDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                prefix => new TagHelperPrefixDirectiveChunkGenerator(prefix));
        }

        protected virtual void AddTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                lookupText => new AddTagHelperChunkGenerator(lookupText));
        }

        protected virtual void RemoveTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.RemoveTagHelperKeyword,
                lookupText => new RemoveTagHelperChunkGenerator(lookupText));
        }

        protected virtual void SectionDirective()
        {
            var nested = Context.Builder.ActiveBlocks.Any(block => block.Type == BlockType.Section);
            var errorReported = false;

            // Set the block and span type
            Context.Builder.CurrentBlock.Type = BlockType.Section;

            // Verify we're on "section" and accept
            AssertDirective(SyntaxConstants.CSharp.SectionKeyword);
            var startLocation = CurrentLocation;
            AcceptAndMoveNext();

            if (nested)
            {
                Context.ErrorSink.OnError(
                    startLocation,
                    LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                    Span.GetContent().Value.Length);
                errorReported = true;
            }

            var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the section name
            var sectionName = string.Empty;
            if (!Required(CSharpSymbolType.Identifier,
                          errorIfNotFound: true,
                          errorBase: LegacyResources.FormatParseError_Unexpected_Character_At_Section_Name_Start))
            {
                if (!errorReported)
                {
                    errorReported = true;
                }

                PutCurrentBack();
                PutBack(whitespace);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
            }
            else
            {
                Accept(whitespace);
                sectionName = CurrentSymbol.Content;
                AcceptAndMoveNext();
            }
            Context.Builder.CurrentBlock.ChunkGenerator = new SectionChunkGenerator(sectionName);

            var errorLocation = CurrentLocation;
            whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the starting brace
            var sawStartingBrace = At(CSharpSymbolType.LeftBrace);
            if (!sawStartingBrace)
            {
                if (!errorReported)
                {
                    errorReported = true;
                    Context.ErrorSink.OnError(
                        errorLocation,
                        LegacyResources.ParseError_MissingOpenBraceAfterSection,
                        length: 1  /* { */);
                }

                PutCurrentBack();
                PutBack(whitespace);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
                Optional(CSharpSymbolType.NewLine);
                Output(SpanKind.MetaCode);
                CompleteBlock();
                return;
            }
            else
            {
                Accept(whitespace);
            }

            var startingBraceLocation = CurrentLocation;

            // Set up edit handler
            var editHandler = new AutoCompleteEditHandler(Language.TokenizeString, autoCompleteAtEndOfSpan: true);

            Span.EditHandler = editHandler;
            Span.Accept(CurrentSymbol);

            // Output Metacode then switch to section parser
            Output(SpanKind.MetaCode);
            SectionBlock("{", "}", caseSensitive: true);

            Span.ChunkGenerator = SpanChunkGenerator.Null;
            // Check for the terminating "}"
            if (!Optional(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.ErrorSink.OnError(
                    startingBraceLocation,
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(
                        SyntaxConstants.CSharp.SectionKeyword,
                        Language.GetSample(CSharpSymbolType.RightBrace),
                        Language.GetSample(CSharpSymbolType.LeftBrace)),
                    length: 1 /* } */);
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            CompleteBlock(insertMarkerIfNecessary: false, captureWhitespaceToEndOfLine: true);
            Output(SpanKind.MetaCode);
            return;
        }

        protected virtual void FunctionsDirective()
        {
            // Set the block type
            Context.Builder.CurrentBlock.Type = BlockType.Functions;

            // Verify we're on "functions" and accept
            AssertDirective(SyntaxConstants.CSharp.FunctionsKeyword);
            var block = new Block(CurrentSymbol);
            AcceptAndMoveNext();

            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            if (!At(CSharpSymbolType.LeftBrace))
            {
                Context.ErrorSink.OnError(
                    CurrentLocation,
                    LegacyResources.FormatParseError_Expected_X(Language.GetSample(CSharpSymbolType.LeftBrace)),
                    length: 1 /* { */);
                CompleteBlock();
                Output(SpanKind.MetaCode);
                return;
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            // Capture start point and continue
            var blockStart = CurrentLocation;
            AcceptAndMoveNext();

            // Output what we've seen and continue
            Output(SpanKind.MetaCode);

            var editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;

            Balance(BalancingModes.NoErrorOnFailure, CSharpSymbolType.LeftBrace, CSharpSymbolType.RightBrace, blockStart);
            Span.ChunkGenerator = new TypeMemberChunkGenerator();
            if (!At(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.ErrorSink.OnError(
                    blockStart,
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(block.Name, "}", "{"),
                    length: 1 /* } */);
                CompleteBlock();
                Output(SpanKind.Code);
            }
            else
            {
                Output(SpanKind.Code);
                Assert(CSharpSymbolType.RightBrace);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                AcceptAndMoveNext();
                CompleteBlock();
                Output(SpanKind.MetaCode);
            }
        }

        protected virtual void InheritsDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(SyntaxConstants.CSharp.InheritsKeyword);
            AcceptAndMoveNext();

            InheritsDirectiveCore();
        }

        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Assert(CSharpSymbolType.Identifier);
            Debug.Assert(string.Equals(CurrentSymbol.Content, directive, StringComparison.Ordinal));
        }

        protected void InheritsDirectiveCore()
        {
            BaseTypeDirective(
                LegacyResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName,
                baseType => new SetBaseTypeChunkGenerator(baseType));
        }

        protected void BaseTypeDirective(string noTypeNameError, Func<string, SpanChunkGenerator> createChunkGenerator)
        {
            var keywordStartLocation = Span.Start;

            // Set the block type
            Context.Builder.CurrentBlock.Type = BlockType.Directive;

            var keywordLength = Span.GetContent().Value.Length;

            // Accept whitespace
            var remainingWhitespace = AcceptSingleWhiteSpaceCharacter();

            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Output(SpanKind.MetaCode);

            if (remainingWhitespace != null)
            {
                Accept(remainingWhitespace);
            }

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (EndOfFile || At(CSharpSymbolType.WhiteSpace) || At(CSharpSymbolType.NewLine))
            {
                Context.ErrorSink.OnError(
                    keywordStartLocation,
                    noTypeNameError,
                    keywordLength);
            }

            // Parse to the end of the line
            AcceptUntil(CSharpSymbolType.NewLine);
            if (!Context.DesignTimeMode)
            {
                // We want the newline to be treated as code, but it causes issues at design-time.
                Optional(CSharpSymbolType.NewLine);
            }

            // Pull out the type name
            string baseType = Span.GetContent();

            // Set up chunk generation
            Span.ChunkGenerator = createChunkGenerator(baseType.Trim());

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);
        }

        private void TagHelperDirective(string keyword, Func<string, ISpanChunkGenerator> chunkGeneratorFactory)
        {
            AssertDirective(keyword);
            var keywordStartLocation = CurrentLocation;

            // Accept the directive name
            AcceptAndMoveNext();

            // Set the block type
            Context.Builder.CurrentBlock.Type = BlockType.Directive;

            var keywordLength = Span.GetContent().Value.Length;

            var foundWhitespace = At(CSharpSymbolType.WhiteSpace);
            AcceptWhile(CSharpSymbolType.WhiteSpace);

            // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
            // to the document.  We can't accept it.
            Output(SpanKind.MetaCode, foundWhitespace ? AcceptedCharacters.None : AcceptedCharacters.AnyExceptNewline);

            ISpanChunkGenerator chunkGenerator;
            if (EndOfFile || At(CSharpSymbolType.NewLine))
            {
                Context.ErrorSink.OnError(
                    keywordStartLocation,
                    LegacyResources.FormatParseError_DirectiveMustHaveValue(keyword),
                    keywordLength);

                chunkGenerator = chunkGeneratorFactory(string.Empty);
            }
            else
            {
                // Need to grab the current location before we accept until the end of the line.
                var startLocation = CurrentLocation;

                // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                // etc.
                AcceptUntil(CSharpSymbolType.NewLine);

                // Pull out the value and remove whitespaces and optional quotes
                var rawValue = Span.GetContent().Value.Trim();

                var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                if (startsWithQuote != endsWithQuote)
                {
                    Context.ErrorSink.OnError(
                        startLocation,
                        LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(keyword),
                        rawValue.Length);
                }
                else if (startsWithQuote)
                {
                    if (rawValue.Length > 2)
                    {
                        // Remove extra quotes
                        rawValue = rawValue.Substring(1, rawValue.Length - 2);
                    }
                    else
                    {
                        // raw value is only quotes
                        rawValue = string.Empty;
                    }
                }

                chunkGenerator = chunkGeneratorFactory(rawValue);
            }

            Span.ChunkGenerator = chunkGenerator;

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);
        }

        protected class Block
        {
            public Block(string name, SourceLocation start)
            {
                Name = name;
                Start = start;
            }

            public Block(CSharpSymbol symbol)
                : this(GetName(symbol), symbol.Start)
            {
            }

            public string Name { get; set; }
            public SourceLocation Start { get; set; }

            private static string GetName(CSharpSymbol sym)
            {
                if (sym.Type == CSharpSymbolType.Keyword)
                {
                    return CSharpLanguageCharacteristics.GetKeyword(sym.Keyword.Value);
                }
                return sym.Content;
            }
        }
    }
}
