// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>
    {
        private static readonly Func<CSharpSymbol, bool> IsValidStatementSpacingSymbol =
            IsSpacingToken(includeNewLines: true, includeComments: true);

        internal static readonly DirectiveDescriptor SectionDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.SectionKeyword,
            DirectiveKind.RazorBlock,
            builder => builder.AddMemberToken());

        internal static readonly DirectiveDescriptor FunctionsDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.FunctionsKeyword,
            DirectiveKind.CodeBlock);

        internal static readonly DirectiveDescriptor InheritsDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.InheritsKeyword,
            DirectiveKind.SingleLine,
            builder => builder.AddTypeToken());

        internal static readonly DirectiveDescriptor AddTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.AddTagHelperKeyword,
            DirectiveKind.SingleLine,
            builder => builder.AddStringToken());

        internal static readonly DirectiveDescriptor RemoveTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.RemoveTagHelperKeyword,
            DirectiveKind.SingleLine,
            builder => builder.AddStringToken());

        internal static readonly DirectiveDescriptor TagHelperPrefixDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
            DirectiveKind.SingleLine,
            builder => builder.AddStringToken());

        internal static readonly IEnumerable<DirectiveDescriptor> DefaultDirectiveDescriptors = new[]
        {
            SectionDirectiveDescriptor,
            FunctionsDirectiveDescriptor,
            InheritsDirectiveDescriptor,
        };

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

        private readonly ISet<string> CurrentKeywords = new HashSet<string>(DefaultKeywords);

        private Dictionary<string, Action> _directiveParsers = new Dictionary<string, Action>(StringComparer.Ordinal);
        private Dictionary<CSharpKeyword, Action<bool>> _keywordParsers = new Dictionary<CSharpKeyword, Action<bool>>();

        public CSharpCodeParser(ParserContext context)
            : this(directiveDescriptors: Enumerable.Empty<DirectiveDescriptor>(), context: context)
        {
        }

        public CSharpCodeParser(IEnumerable<DirectiveDescriptor> directiveDescriptors, ParserContext context)
            : base(context.ParseOnlyLeadingDirectives ? FirstDirectiveCSharpLanguageCharacteristics.Instance : CSharpLanguageCharacteristics.Instance, context)
        {
            Keywords = new HashSet<string>();
            SetUpKeywords();
            SetupDirectives(directiveDescriptors);
            SetUpExpressions();
        }

        public HtmlMarkupParser HtmlParser { get; set; }

        protected internal ISet<string> Keywords { get; private set; }

        public bool IsNested { get; set; }

        protected override bool SymbolTypeEquals(CSharpSymbolType x, CSharpSymbolType y) => x == y;

        protected void MapDirectives(Action handler, params string[] directives)
        {
            foreach (var directive in directives)
            {
                _directiveParsers.Add(directive, () =>
                {
                    EnsureDirectiveIsAtStartOfLine();
                    handler();
                });
                Keywords.Add(directive);

                // These C# keywords are reserved for use in directives. It's an error to use them outside of
                // a directive. This code removes the error generation if the directive *is* registered.
                if (string.Equals(directive, "class", StringComparison.OrdinalIgnoreCase))
                {
                    _keywordParsers.Remove(CSharpKeyword.Class);
                }
                else if (string.Equals(directive, "namespace", StringComparison.OrdinalIgnoreCase))
                {
                    _keywordParsers.Remove(CSharpKeyword.Namespace);
                }
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
            foreach (var keyword in keywords)
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

                Span.Start = CurrentLocation;

                // Unless changed, the block is a statement block
                using (Context.Builder.StartBlock(BlockKindInternal.Statement))
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

                        // Back up to the end of the transition
                        Context.Source.Position -= split.Item2.Content.Length;
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
                            Output(SpanKindInternal.Code);
                        }
                        AtTransition(current);
                    }
                    else
                    {
                        // No "@" => Jump straight to AfterTransition
                        AfterTransition();
                    }

                    Output(SpanKindInternal.Code);
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
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;

            // Output the "@" span and continue here
            Output(SpanKindInternal.Transition);
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
                            Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                            Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                            ExplicitExpression();
                            return;
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.Identifier)
                        {
                            if (TryGetDirectiveHandler(CurrentSymbol.Content, out var handler))
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
                                        CurrentStart,
                                        LegacyResources.FormatParseError_HelperDirectiveNotAvailable(
                                            SyntaxConstants.CSharp.HelperKeyword),
                                        CurrentSymbol.Content.Length);
                                }

                                Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                                Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                                ImplicitExpression();
                                return;
                            }
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.Keyword)
                        {
                            if (TryGetDirectiveHandler(CurrentSymbol.Content, out var handler))
                            {
                                Span.ChunkGenerator = SpanChunkGenerator.Null;
                                handler();
                                return;
                            }
                            else
                            {
                                KeywordBlock(topLevel: true);
                                return;
                            }
                        }
                        else if (CurrentSymbol.Type == CSharpSymbolType.LeftBrace)
                        {
                            VerbatimBlock();
                            return;
                        }
                    }

                    // Invalid character
                    Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                    Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                    AddMarkerSymbolIfNecessary();
                    Span.ChunkGenerator = new ExpressionChunkGenerator();
                    Span.EditHandler = new ImplicitExpressionEditHandler(
                        Language.TokenizeString,
                        CurrentKeywords,
                        acceptTrailingDot: IsNested)
                    {
                        AcceptedCharacters = AcceptedCharactersInternal.NonWhiteSpace
                    };
                    if (At(CSharpSymbolType.WhiteSpace) || At(CSharpSymbolType.NewLine))
                    {
                        Context.ErrorSink.OnError(
                            CurrentStart,
                            LegacyResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS,
                            CurrentSymbol.Content.Length);
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            CurrentStart,
                            LegacyResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                            length: 1 /* end of file */);
                    }
                    else
                    {
                        Context.ErrorSink.OnError(
                            CurrentStart,
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
            var block = new Block(LegacyResources.BlockName_Code, CurrentStart);
            AcceptAndMoveNext();

            // Set up the "{" span and output
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Output(SpanKindInternal.MetaCode);

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
            Output(SpanKindInternal.Code);

            if (Optional(CSharpSymbolType.RightBrace))
            {
                // Set up the "}" span
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
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

            Output(SpanKindInternal.MetaCode);
        }

        private void ImplicitExpression()
        {
            ImplicitExpression(AcceptedCharactersInternal.NonWhiteSpace);
        }

        // Async implicit expressions include the "await" keyword and therefore need to allow spaces to
        // separate the "await" and the following code.
        private void AsyncImplicitExpression()
        {
            ImplicitExpression(AcceptedCharactersInternal.AnyExceptNewline);
        }

        private void ImplicitExpression(AcceptedCharactersInternal acceptedCharacters)
        {
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
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
                Output(SpanKindInternal.Code);
            }
        }

        private bool MethodCallOrArrayIndex(AcceptedCharactersInternal acceptedCharacters)
        {
            if (!EndOfFile)
            {
                if (CurrentSymbol.Type == CSharpSymbolType.LeftParenthesis ||
                    CurrentSymbol.Type == CSharpSymbolType.LeftBracket)
                {
                    // If we end within "(", whitespace is fine
                    Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;

                    CSharpSymbolType right;
                    bool success;

                    using (PushSpanConfig((span, prev) =>
                    {
                        prev(span);
                        span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
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
            if (insertMarkerIfNecessary && Context.Builder.LastAcceptedCharacters != AcceptedCharactersInternal.Any)
            {
                AddMarkerSymbolIfNecessary();
            }

            EnsureCurrent();

            // Read whitespace, but not newlines
            // If we're not inserting a marker span, we don't need to capture whitespace
            if (!Context.WhiteSpaceIsSignificantToAncestorBlock &&
                Context.Builder.CurrentBlock.Type != BlockKindInternal.Expression &&
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
            var whitespace = ReadWhile(sym => sym.Type == CSharpSymbolType.WhiteSpace);
            if (At(CSharpSymbolType.NewLine))
            {
                Accept(whitespace);
                AcceptAndMoveNext();
                PutCurrentBack();
            }
            else
            {
                PutCurrentBack();
                PutBack(whitespace);
            }
        }

        private void ConfigureExplicitExpressionSpan(SpanBuilder sb)
        {
            sb.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
            sb.ChunkGenerator = new ExpressionChunkGenerator();
        }

        private void ExplicitExpression()
        {
            var block = new Block(LegacyResources.BlockName_ExplicitExpression, CurrentStart);
            Assert(CSharpSymbolType.LeftParenthesis);
            AcceptAndMoveNext();
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Output(SpanKindInternal.MetaCode);
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
                    Accept(new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown));
                }

                // Output the content span and then capture the ")"
                Output(SpanKindInternal.Code);
            }
            Optional(CSharpSymbolType.RightParenthesis);
            if (!EndOfFile)
            {
                PutCurrentBack();
            }
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            CompleteBlock(insertMarkerIfNecessary: false);
            Output(SpanKindInternal.MetaCode);
        }

        private void Template()
        {
            if (Context.Builder.ActiveBlocks.Any(block => block.Type == BlockKindInternal.Template))
            {
                Context.ErrorSink.OnError(
                    CurrentStart,
                    LegacyResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
                    length: 1 /* @ */);
            }
            Output(SpanKindInternal.Code);
            using (Context.Builder.StartBlock(BlockKindInternal.Template))
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
            ParseWithOtherParser(p => p.ParseRazorBlock(Tuple.Create(left, right), caseSensitive));
        }

        private void NestedBlock()
        {
            Output(SpanKindInternal.Code);

            var wasNested = IsNested;
            IsNested = true;
            using (PushSpanConfig())
            {
                ParseBlock();
            }

            Span.Start = CurrentLocation;
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

            Span.Start = CurrentLocation;
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
            MapKeywords(ReservedDirective, CSharpKeyword.Class, CSharpKeyword.Namespace);
        }

        protected virtual void ReservedDirective(bool topLevel)
        {
            Context.ErrorSink.OnError(
                CurrentStart,
                LegacyResources.FormatParseError_ReservedWord(CurrentSymbol.Content),
                CurrentSymbol.Content.Length);
            AcceptAndMoveNext();
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            Span.ChunkGenerator = SpanChunkGenerator.Null;
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;
            CompleteBlock();
            Output(SpanKindInternal.MetaCode);
        }

        private void KeywordBlock(bool topLevel)
        {
            HandleKeyword(topLevel, () =>
            {
                Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
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
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
            var whitespace = SkipToNextImportantToken();

            if (At(CSharpKeyword.While))
            {
                Accept(whitespace);
                Assert(CSharpKeyword.While);
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (AcceptCondition() && Optional(CSharpSymbolType.Semicolon))
                {
                    Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                }
            }
            else
            {
                PutCurrentBack();
                PutBack(whitespace);
            }
        }

        private void UsingKeyword(bool topLevel)
        {
            Assert(CSharpKeyword.Using);
            var block = new Block(CurrentSymbol, CurrentStart);
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
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;

            var start = CurrentStart;
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

            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.AnyExceptNewline;
            Span.ChunkGenerator = new AddImportChunkGenerator(new LocationTagged<string>(
                string.Concat(Span.Symbols.Skip(1).Select(s => s.Content)),
                start));

            // Optional ";"
            if (EnsureCurrent())
            {
                Optional(CSharpSymbolType.Semicolon);
            }
        }

        // Used for parsing a qualified name like that which follows the `namespace` keyword.
        //
        // qualified-identifier:
        //      identifier
        //      qualified-identifier . identifier
        protected bool QualifiedIdentifier(out int identifierLength)
        {
            var currentIdentifierLength = 0;
            var expectingDot = false;
            var tokens = ReadWhile(token =>
            {
                var type = token.Type;
                if ((expectingDot && type == CSharpSymbolType.Dot) ||
                    (!expectingDot && type == CSharpSymbolType.Identifier))
                {
                    expectingDot = !expectingDot;
                    return true;
                }

                if (type != CSharpSymbolType.WhiteSpace &&
                    type != CSharpSymbolType.NewLine)
                {
                    expectingDot = false;
                    currentIdentifierLength += token.Content.Length;
                }

                return false;
            });

            identifierLength = currentIdentifierLength;
            var validQualifiedIdentifier = expectingDot;
            if (validQualifiedIdentifier)
            {
                foreach (var token in tokens)
                {
                    identifierLength += token.Content.Length;
                    Accept(token);
                }

                return true;
            }
            else
            {
                PutCurrentBack();

                foreach (var token in tokens)
                {
                    identifierLength += token.Content.Length;
                    PutBack(token);
                }

                EnsureCurrent();
                return false;
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
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
            }
        }

        private void AfterIfClause()
        {
            // Grab whitespace and razor comments
            var whitespace = SkipToNextImportantToken();

            // Check for an else part
            if (At(CSharpKeyword.Else))
            {
                Accept(whitespace);
                Assert(CSharpKeyword.Else);
                ElseClause();
            }
            else
            {
                // No else, return whitespace
                PutCurrentBack();
                PutBack(whitespace);
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
            }
        }

        private void ElseClause()
        {
            if (!At(CSharpKeyword.Else))
            {
                return;
            }
            var block = new Block(CurrentSymbol, CurrentStart);

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
                        CurrentStart,
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
            var block = new Block(CurrentSymbol, CurrentStart);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
            ExpectCodeBlock(block);
        }

        private void FilterableCatchBlock()
        {
            Assert(CSharpKeyword.Catch);

            var block = new Block(CurrentSymbol, CurrentStart);

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
            var block = new Block(CurrentSymbol, CurrentStart);
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
            Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;

            // Accept whitespace but always keep the last whitespace node so we can put it back if necessary
            var lastWhitespace = AcceptWhiteSpaceInLines();

            if (EndOfFile)
            {
                if (lastWhitespace != null)
                {
                    Accept(lastWhitespace);
                }
                return;
            }

            var type = CurrentSymbol.Type;
            var loc = CurrentStart;

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
                Output(SpanKindInternal.Code);
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
                    Output(SpanKindInternal.Code);
                    RazorComment();
                    Statement(block);
                    break;
                case CSharpSymbolType.LeftBrace:
                    // Verbatim Block
                    block = block ?? new Block(LegacyResources.BlockName_Code, CurrentStart);
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
                Output(SpanKindInternal.Code);

                // Output "@" as hidden span
                Accept(transition);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.Code);

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
                        CurrentStart,
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
                var bookmark = CurrentStart.AbsoluteIndex;
                var read = ReadWhile(sym =>
                    sym.Type != CSharpSymbolType.Semicolon &&
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
                    Output(SpanKindInternal.Code);
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
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                AcceptAndMoveNext();
            }
        }

        private void HandleKeyword(bool topLevel, Action fallback)
        {
            Debug.Assert(CurrentSymbol.Type == CSharpSymbolType.Keyword && CurrentSymbol.Keyword != null);
            if (_keywordParsers.TryGetValue(CurrentSymbol.Keyword.Value, out var handler))
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
                var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (At(CSharpSymbolType.RazorCommentTransition))
                {
                    Accept(whitespace);
                    Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                    RazorComment();
                }
                else
                {
                    return whitespace;
                }
            }
            return Enumerable.Empty<CSharpSymbol>();
        }

        // Common code for Parsers, but FxCop REALLY doesn't like it in the base class.. moving it here for now.
        protected override void OutputSpanBeforeRazorComment()
        {
            AddMarkerSymbolIfNecessary();
            Output(SpanKindInternal.Code);
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

        private void SetupDirectives(IEnumerable<DirectiveDescriptor> directiveDescriptors)
        {
            var allDirectives = directiveDescriptors.Concat(DefaultDirectiveDescriptors).ToList();

            for (var i = 0; i < allDirectives.Count; i++)
            {
                var directiveDescriptor = allDirectives[i];
                CurrentKeywords.Add(directiveDescriptor.Directive);
                MapDirectives(() => HandleDirective(directiveDescriptor), directiveDescriptor.Directive);
            }

            MapDirectives(TagHelperPrefixDirective, SyntaxConstants.CSharp.TagHelperPrefixKeyword);
            MapDirectives(AddTagHelperDirective, SyntaxConstants.CSharp.AddTagHelperKeyword);
            MapDirectives(RemoveTagHelperDirective, SyntaxConstants.CSharp.RemoveTagHelperKeyword);
        }

        private void EnsureDirectiveIsAtStartOfLine()
        {
            // 1 is the offset of the @ transition for the directive.
            if (CurrentStart.CharacterIndex > 1)
            {
                var index = CurrentStart.AbsoluteIndex - 1;
                var lineStart = CurrentStart.AbsoluteIndex - CurrentStart.CharacterIndex;
                while (--index >= lineStart)
                {
                    var @char = Context.SourceDocument[index];

                    if (!char.IsWhiteSpace(@char))
                    {
                        var currentDirective = CurrentSymbol.Content;
                        Context.ErrorSink.OnError(
                            CurrentStart,
                            Resources.FormatDirectiveMustAppearAtStartOfLine(currentDirective),
                            length: currentDirective.Length);
                        break;
                    }
                }
            }
        }

        private void HandleDirective(DirectiveDescriptor descriptor)
        {
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;
            Context.Builder.CurrentBlock.ChunkGenerator = new DirectiveChunkGenerator(descriptor);
            AssertDirective(descriptor.Directive);

            AcceptAndMoveNext();
            Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

            for (var i = 0; i < descriptor.Tokens.Count; i++)
            {
                var tokenDescriptor = descriptor.Tokens[i];
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

                if (tokenDescriptor.Kind == DirectiveTokenKind.Member ||
                    tokenDescriptor.Kind == DirectiveTokenKind.Namespace ||
                    tokenDescriptor.Kind == DirectiveTokenKind.Type)
                {
                    Span.ChunkGenerator = SpanChunkGenerator.Null;
                    Output(SpanKindInternal.Code, AcceptedCharactersInternal.WhiteSpace);
                }
                else
                {
                    Span.ChunkGenerator = SpanChunkGenerator.Null;
                    Output(SpanKindInternal.Markup, AcceptedCharactersInternal.WhiteSpace);
                }

                if (tokenDescriptor.Optional && (EndOfFile || At(CSharpSymbolType.NewLine)))
                {
                    break;
                }
                else if (EndOfFile)
                {
                    Context.ErrorSink.OnError(
                        CurrentStart,
                        LegacyResources.FormatUnexpectedEOFAfterDirective(descriptor.Directive, tokenDescriptor.Kind.ToString().ToLowerInvariant()),
                        length: 1);
                    return;
                }

                switch (tokenDescriptor.Kind)
                {
                    case DirectiveTokenKind.Type:
                        if (!NamespaceOrTypeName())
                        {
                            Context.ErrorSink.OnError(
                                CurrentStart,
                                LegacyResources.FormatDirectiveExpectsTypeName(descriptor.Directive),
                                CurrentSymbol.Content.Length);

                            return;
                        }
                        break;

                    case DirectiveTokenKind.Namespace:
                        if (!QualifiedIdentifier(out var identifierLength))
                        {
                            Context.ErrorSink.OnError(
                                CurrentStart,
                                LegacyResources.FormatDirectiveExpectsNamespace(descriptor.Directive),
                                identifierLength);

                            return;
                        }
                        break;

                    case DirectiveTokenKind.Member:
                        if (At(CSharpSymbolType.Identifier))
                        {
                            AcceptAndMoveNext();
                        }
                        else
                        {
                            Context.ErrorSink.OnError(
                                CurrentStart,
                                LegacyResources.FormatDirectiveExpectsIdentifier(descriptor.Directive),
                                CurrentSymbol.Content.Length);
                            return;
                        }
                        break;

                    case DirectiveTokenKind.String:
                        if (At(CSharpSymbolType.StringLiteral) && CurrentSymbol.Errors.Count == 0)
                        {
                            AcceptAndMoveNext();
                        }
                        else
                        {
                            Context.ErrorSink.OnError(
                                CurrentStart,
                                LegacyResources.FormatDirectiveExpectsQuotedStringLiteral(descriptor.Directive),
                                CurrentSymbol.Content.Length);
                            return;
                        }
                        break;
                }

                Span.ChunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                Span.EditHandler = new DirectiveTokenEditHandler(Language.TokenizeString);
                Output(SpanKindInternal.Code, AcceptedCharactersInternal.NonWhiteSpace);
            }

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));
            Span.ChunkGenerator = SpanChunkGenerator.Null;

            switch (descriptor.Kind)
            {
                case DirectiveKind.SingleLine:
                    Optional(CSharpSymbolType.Semicolon);
                    AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

                    if (At(CSharpSymbolType.NewLine))
                    {
                        AcceptAndMoveNext();
                    }
                    else if (!EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            CurrentStart,
                            LegacyResources.FormatUnexpectedDirectiveLiteral(descriptor.Directive, LegacyResources.ErrorComponent_Newline),
                            CurrentSymbol.Content.Length);
                    }

                    Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.WhiteSpace);
                    break;
                case DirectiveKind.RazorBlock:
                    AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                    Output(SpanKindInternal.Markup, AcceptedCharactersInternal.AllWhiteSpace);

                    ParseDirectiveBlock(descriptor, parseChildren: (startingBraceLocation) =>
                    {
                        // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
                        // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
                        // as C#; it should be handled as a period because it's wrapped in markup.
                        var wasNested = IsNested;
                        IsNested = false;

                        using (PushSpanConfig())
                        {
                            HtmlParser.ParseRazorBlock(Tuple.Create("{", "}"), caseSensitive: true);
                        }

                        Span.Start = CurrentLocation;
                        Initialize(Span);

                        IsNested = wasNested;

                        NextToken();
                    });
                    break;
                case DirectiveKind.CodeBlock:
                    AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                    Output(SpanKindInternal.Markup, AcceptedCharactersInternal.AllWhiteSpace);

                    ParseDirectiveBlock(descriptor, parseChildren: (startingBraceLocation) =>
                    {
                        NextToken();
                        Balance(BalancingModes.NoErrorOnFailure, CSharpSymbolType.LeftBrace, CSharpSymbolType.RightBrace, startingBraceLocation);
                        Span.ChunkGenerator = new StatementChunkGenerator();
                        Output(SpanKindInternal.Code);
                    });
                    break;
            }
        }

        private void ParseDirectiveBlock(DirectiveDescriptor descriptor, Action<SourceLocation> parseChildren)
        {
            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    CurrentStart,
                    LegacyResources.FormatUnexpectedEOFAfterDirective(descriptor.Directive, "{"),
                    length: 1 /* { */);
            }
            else if (!At(CSharpSymbolType.LeftBrace))
            {
                Context.ErrorSink.OnError(
                    CurrentStart,
                    LegacyResources.FormatUnexpectedDirectiveLiteral(descriptor.Directive, "{"),
                    CurrentSymbol.Content.Length);
            }
            else
            {
                var editHandler = new AutoCompleteEditHandler(Language.TokenizeString, autoCompleteAtEndOfSpan: true);
                Span.EditHandler = editHandler;
                var startingBraceLocation = CurrentStart;
                Accept(CurrentSymbol);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

                parseChildren(startingBraceLocation);

                Span.ChunkGenerator = SpanChunkGenerator.Null;
                if (!Optional(CSharpSymbolType.RightBrace))
                {
                    editHandler.AutoCompleteString = "}";
                    Context.ErrorSink.OnError(
                        startingBraceLocation,
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(descriptor.Directive, "}", "{"),
                        length: 1 /* } */);
                }
                else
                {
                    Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                }
                CompleteBlock(insertMarkerIfNecessary: false, captureWhitespaceToEndOfLine: true);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);
            }
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

        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Debug.Assert(CurrentSymbol.Type == CSharpSymbolType.Identifier || CurrentSymbol.Type == CSharpSymbolType.Keyword);
            Debug.Assert(string.Equals(CurrentSymbol.Content, directive, StringComparison.Ordinal));
        }

        protected void BaseTypeDirective(string noTypeNameError, Func<string, SpanChunkGenerator> createChunkGenerator)
        {
            var keywordStartLocation = Span.Start;

            // Set the block type
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;

            var keywordLength = Span.End.AbsoluteIndex - Span.Start.AbsoluteIndex;

            // Accept whitespace
            var remainingWhitespace = AcceptSingleWhiteSpaceCharacter();

            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }

            Output(SpanKindInternal.MetaCode);

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
            var baseType = string.Concat(Span.Symbols.Select(s => s.Content));

            // Set up chunk generation
            Span.ChunkGenerator = createChunkGenerator(baseType.Trim());

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKindInternal.Code, AcceptedCharactersInternal.AnyExceptNewline);
        }

        private void TagHelperDirective(string keyword, Func<string, ISpanChunkGenerator> chunkGeneratorFactory)
        {
            AssertDirective(keyword);
            var keywordStartLocation = CurrentStart;

            // Accept the directive name
            AcceptAndMoveNext();

            // Set the block type
            Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;

            var keywordLength = Span.End.AbsoluteIndex - Span.Start.AbsoluteIndex;

            var foundWhitespace = At(CSharpSymbolType.WhiteSpace);
            AcceptWhile(CSharpSymbolType.WhiteSpace);

            // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
            // to the document.  We can't accept it.
            Output(SpanKindInternal.MetaCode, foundWhitespace ? AcceptedCharactersInternal.None : AcceptedCharactersInternal.AnyExceptNewline);

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
                var startLocation = CurrentStart;

                // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                // etc.
                AcceptUntil(CSharpSymbolType.NewLine);

                // Pull out the value and remove whitespaces and optional quotes
                var rawValue = string.Concat(Span.Symbols.Select(s => s.Content)).Trim();

                var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                if (startsWithQuote != endsWithQuote)
                {
                    Context.ErrorSink.OnError(
                        startLocation,
                        LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(keyword),
                        rawValue.Length);
                }

                chunkGenerator = chunkGeneratorFactory(rawValue);
            }

            Span.ChunkGenerator = chunkGenerator;

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKindInternal.Code, AcceptedCharactersInternal.AnyExceptNewline);
        }

        protected class Block
        {
            public Block(string name, SourceLocation start)
            {
                Name = name;
                Start = start;
            }

            public Block(CSharpSymbol symbol, SourceLocation start)
                : this(GetName(symbol), start)
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
