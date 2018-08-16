// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer>
    {
        private static HashSet<char> InvalidNonWhitespaceNameCharacters = new HashSet<char>(new[]
        {
            '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*'
        });

        private static readonly Func<SyntaxToken, bool> IsValidStatementSpacingToken =
            IsSpacingToken(includeNewLines: true, includeComments: true);

        internal static readonly DirectiveDescriptor AddTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.AddTagHelperKeyword,
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddStringToken(Resources.AddTagHelperDirective_StringToken_Name, Resources.AddTagHelperDirective_StringToken_Description);
                builder.Description = Resources.AddTagHelperDirective_Description;
            });

        internal static readonly DirectiveDescriptor RemoveTagHelperDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.RemoveTagHelperKeyword,
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddStringToken(Resources.RemoveTagHelperDirective_StringToken_Name, Resources.RemoveTagHelperDirective_StringToken_Description);
                builder.Description = Resources.RemoveTagHelperDirective_Description;
            });

        internal static readonly DirectiveDescriptor TagHelperPrefixDirectiveDescriptor = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddStringToken(Resources.TagHelperPrefixDirective_PrefixToken_Name, Resources.TagHelperPrefixDirective_PrefixToken_Description);
                builder.Description = Resources.TagHelperPrefixDirective_Description;
            });

        internal static readonly IEnumerable<DirectiveDescriptor> DefaultDirectiveDescriptors = new DirectiveDescriptor[]
        {
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
            "namespace",
            "class",
        };

        private readonly ISet<string> CurrentKeywords = new HashSet<string>(DefaultKeywords);

        private Dictionary<string, Action> _directiveParsers = new Dictionary<string, Action>(StringComparer.Ordinal);
        private Dictionary<CSharpKeyword, Action<bool>> _keywordParsers = new Dictionary<CSharpKeyword, Action<bool>>();

        public CSharpCodeParser(ParserContext context)
            : this(directives: Enumerable.Empty<DirectiveDescriptor>(), context: context)
        {
        }

        public CSharpCodeParser(IEnumerable<DirectiveDescriptor> directives, ParserContext context)
            : base(context.ParseLeadingDirectives ? FirstDirectiveCSharpLanguageCharacteristics.Instance : CSharpLanguageCharacteristics.Instance, context)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Keywords = new HashSet<string>();
            SetUpKeywords();
            SetupDirectives(directives);
            SetUpExpressions();
        }

        public HtmlMarkupParser HtmlParser { get; set; }

        protected internal ISet<string> Keywords { get; private set; }

        public bool IsNested { get; set; }

        protected override bool TokenKindEquals(SyntaxKind x, SyntaxKind y) => x == y;

        protected void MapDirectives(Action handler, params string[] directives)
        {
            foreach (var directive in directives)
            {
                _directiveParsers.Add(directive, () =>
                {
                    handler();
                    Context.SeenDirectives.Add(directive);
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
            var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
            Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword &&
                result.HasValue &&
                result.Value == expectedKeyword);
        }

        protected internal bool At(CSharpKeyword keyword)
        {
            var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
            return At(SyntaxKind.Keyword) &&
                result.HasValue &&
                result.Value == keyword;
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

        protected static Func<SyntaxToken, bool> IsSpacingToken(bool includeNewLines, bool includeComments)
        {
            return token => token.Kind == SyntaxKind.Whitespace ||
                          (includeNewLines && token.Kind == SyntaxKind.NewLine) ||
                          (includeComments && token.Kind == SyntaxKind.CSharpComment);
        }

        public override void ParseBlock()
        {
            using (PushSpanConfig(DefaultSpanConfig))
            {
                if (Context == null)
                {
                    throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
                }

                Span.Start = CurrentLocation;

                // Unless changed, the block is a statement block
                using (Context.Builder.StartBlock(BlockKindInternal.Statement))
                {
                    NextToken();

                    AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

                    var current = CurrentToken;
                    if (At(SyntaxKind.StringLiteral) &&
                        CurrentToken.Content.Length > 0 &&
                        CurrentToken.Content[0] == SyntaxConstants.TransitionCharacter)
                    {
                        var split = Language.SplitToken(CurrentToken, 1, SyntaxKind.Transition);
                        current = split.Item1;

                        // Back up to the end of the transition
                        Context.Source.Position -= split.Item2.Content.Length;
                        NextToken();
                    }
                    else if (At(SyntaxKind.Transition))
                    {
                        NextToken();
                    }

                    // Accept "@" if we see it, but if we don't, that's OK. We assume we were started for a good reason
                    if (current.Kind == SyntaxKind.Transition)
                    {
                        if (Span.Tokens.Count > 0)
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

        private void AtTransition(SyntaxToken current)
        {
            Debug.Assert(current.Kind == SyntaxKind.Transition);
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
                        if (CurrentToken.Kind == SyntaxKind.LeftParenthesis)
                        {
                            Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                            Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                            ExplicitExpression();
                            return;
                        }
                        else if (CurrentToken.Kind == SyntaxKind.Identifier)
                        {
                            if (TryGetDirectiveHandler(CurrentToken.Content, out var handler))
                            {
                                Span.ChunkGenerator = SpanChunkGenerator.Null;
                                handler();
                                return;
                            }
                            else
                            {
                                if (string.Equals(
                                    CurrentToken.Content,
                                    SyntaxConstants.CSharp.HelperKeyword,
                                    StringComparison.Ordinal))
                                {
                                    Context.ErrorSink.OnError(
                                        RazorDiagnosticFactory.CreateParsing_HelperDirectiveNotAvailable(
                                            new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                                }

                                Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                                Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                                ImplicitExpression();
                                return;
                            }
                        }
                        else if (CurrentToken.Kind == SyntaxKind.Keyword)
                        {
                            if (TryGetDirectiveHandler(CurrentToken.Content, out var handler))
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
                        else if (CurrentToken.Kind == SyntaxKind.LeftBrace)
                        {
                            VerbatimBlock();
                            return;
                        }
                    }

                    // Invalid character
                    Context.Builder.CurrentBlock.Type = BlockKindInternal.Expression;
                    Context.Builder.CurrentBlock.ChunkGenerator = new ExpressionChunkGenerator();
                    AddMarkerTokenIfNecessary();
                    Span.ChunkGenerator = new ExpressionChunkGenerator();
                    Span.EditHandler = new ImplicitExpressionEditHandler(
                        Language.TokenizeString,
                        CurrentKeywords,
                        acceptTrailingDot: IsNested)
                    {
                        AcceptedCharacters = AcceptedCharactersInternal.NonWhiteSpace
                    };
                    if (At(SyntaxKind.Whitespace) || At(SyntaxKind.NewLine))
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
                    }
                    else
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                CurrentToken.Content));
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
            Assert(SyntaxKind.LeftBrace);
            var block = new Block(Resources.BlockName_Code, CurrentStart);
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
            AddMarkerTokenIfNecessary();
            if (!At(SyntaxKind.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
            }
            Output(SpanKindInternal.Code);

            if (Optional(SyntaxKind.RightBrace))
            {
                // Set up the "}" span
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                Span.ChunkGenerator = SpanChunkGenerator.Null;
            }

            if (!IsNested)
            {
                EnsureCurrent();
                if (At(SyntaxKind.NewLine) ||
                    (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.NewLine)))
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
                if (CurrentToken.Kind == SyntaxKind.LeftParenthesis ||
                    CurrentToken.Kind == SyntaxKind.LeftBracket)
                {
                    // If we end within "(", whitespace is fine
                    Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;

                    SyntaxKind right;
                    bool success;

                    using (PushSpanConfig((span, prev) =>
                    {
                        prev(span);
                        span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                    }))
                    {
                        right = Language.FlipBracket(CurrentToken.Kind);
                        success = Balance(BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                    }

                    if (!success)
                    {
                        AcceptUntil(SyntaxKind.LessThan);
                    }
                    if (At(right))
                    {
                        AcceptAndMoveNext();

                        // At the ending brace, restore the initial accepted characters.
                        Span.EditHandler.AcceptedCharacters = acceptedCharacters;
                    }
                    return MethodCallOrArrayIndex(acceptedCharacters);
                }
                if (At(SyntaxKind.QuestionMark))
                {
                    var next = Lookahead(count: 1);

                    if (next != null)
                    {
                        if (next.Kind == SyntaxKind.Dot)
                        {
                            // Accept null conditional dot operator (?.).
                            AcceptAndMoveNext();
                            AcceptAndMoveNext();

                            // If the next piece after the ?. is a keyword or identifier then we want to continue.
                            return At(SyntaxKind.Identifier) || At(SyntaxKind.Keyword);
                        }
                        else if (next.Kind == SyntaxKind.LeftBracket)
                        {
                            // We're at the ? for a null conditional bracket operator (?[).
                            AcceptAndMoveNext();

                            // Accept the [ and any content inside (it will attempt to balance).
                            return MethodCallOrArrayIndex(acceptedCharacters);
                        }
                    }
                }
                else if (At(SyntaxKind.Dot))
                {
                    var dot = CurrentToken;
                    if (NextToken())
                    {
                        if (At(SyntaxKind.Identifier) || At(SyntaxKind.Keyword))
                        {
                            // Accept the dot and return to the start
                            Accept(dot);
                            return true; // continue
                        }
                        else
                        {
                            // Put the token back
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
                else if (!At(SyntaxKind.Whitespace) && !At(SyntaxKind.NewLine))
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
                AddMarkerTokenIfNecessary();
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
            var whitespace = ReadWhile(token => token.Kind == SyntaxKind.Whitespace);
            if (At(SyntaxKind.NewLine))
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
            var block = new Block(Resources.BlockName_ExplicitExpression, CurrentStart);
            Assert(SyntaxKind.LeftParenthesis);
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
                    SyntaxKind.LeftParenthesis,
                    SyntaxKind.RightParenthesis,
                    block.Start);

                if (!success)
                {
                    AcceptUntil(SyntaxKind.LessThan);
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                            new SourceSpan(block.Start, contentLength: 1 /* ( */), block.Name, ")", "("));
                }

                // If necessary, put an empty-content marker token here
                if (Span.Tokens.Count == 0)
                {
                    Accept(SyntaxFactory.Token(SyntaxKind.Unknown, string.Empty));
                }

                // Output the content span and then capture the ")"
                Output(SpanKindInternal.Code);
            }
            Optional(SyntaxKind.RightParenthesis);
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
                    RazorDiagnosticFactory.CreateParsing_InlineMarkupBlocksCannotBeNested(
                        new SourceSpan(CurrentStart, contentLength: 1 /* @ */)));
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
                   && ((Language.IsTransition(CurrentToken)
                        && NextIs(SyntaxKind.LessThan, SyntaxKind.Colon, SyntaxKind.DoubleColon))
                       || Language.IsCommentStart(CurrentToken));
        }

        protected override void HandleEmbeddedTransition()
        {
            if (Language.IsTransition(CurrentToken))
            {
                PutCurrentBack();
                Template();
            }
            else if (Language.IsCommentStart(CurrentToken))
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
                RazorDiagnosticFactory.CreateParsing_ReservedWord(
                    new SourceSpan(CurrentStart, CurrentToken.Content.Length), CurrentToken.Content));
                
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
            Assert(SyntaxKind.Keyword);
            var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
            Debug.Assert(result.HasValue &&
                         (result.Value == CSharpKeyword.Case ||
                          result.Value == CSharpKeyword.Default));
            AcceptUntil(SyntaxKind.Colon);
            Optional(SyntaxKind.Colon);
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
                if (AcceptCondition() && Optional(SyntaxKind.Semicolon))
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
            var block = new Block(CurrentToken, CurrentStart);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (At(SyntaxKind.LeftParenthesis))
            {
                // using ( ==> Using Statement
                UsingStatement(block);
            }
            else if (At(SyntaxKind.Identifier) || At(CSharpKeyword.Static))
            {
                // using Identifier ==> Using Declaration
                if (!topLevel)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock(
                            new SourceSpan(block.Start, block.Name.Length)));
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
            if (At(SyntaxKind.Identifier))
            {
                // non-static using
                NamespaceOrTypeName();
                var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (At(SyntaxKind.Assign))
                {
                    // Alias
                    Accept(whitespace);
                    Assert(SyntaxKind.Assign);
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
                string.Concat(Span.Tokens.Skip(1).Select(s => s.Content)),
                start));

            // Optional ";"
            if (EnsureCurrent())
            {
                Optional(SyntaxKind.Semicolon);
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
                var type = token.Kind;
                if ((expectingDot && type == SyntaxKind.Dot) ||
                    (!expectingDot && type == SyntaxKind.Identifier))
                {
                    expectingDot = !expectingDot;
                    return true;
                }

                if (type != SyntaxKind.Whitespace &&
                    type != SyntaxKind.NewLine)
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
            if (Optional(SyntaxKind.LeftParenthesis))
            {
                while (!Optional(SyntaxKind.RightParenthesis) && !EndOfFile)
                {
                    Optional(SyntaxKind.Whitespace);

                    if (!NamespaceOrTypeName())
                    {
                        return false;
                    }

                    Optional(SyntaxKind.Whitespace);
                    Optional(SyntaxKind.Identifier);
                    Optional(SyntaxKind.Whitespace);
                    Optional(SyntaxKind.Comma);
                }

                if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
                {
                    // Only accept the whitespace if we are going to consume the next token.
                    AcceptAndMoveNext();
                }

                Optional(SyntaxKind.QuestionMark); // Nullable

                return true;
            }
            else if (Optional(SyntaxKind.Identifier) || Optional(SyntaxKind.Keyword))
            {
                if (Optional(SyntaxKind.DoubleColon))
                {
                    if (!Optional(SyntaxKind.Identifier))
                    {
                        Optional(SyntaxKind.Keyword);
                    }
                }
                if (At(SyntaxKind.LessThan))
                {
                    TypeArgumentList();
                }
                if (Optional(SyntaxKind.Dot))
                {
                    NamespaceOrTypeName();
                }

                if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
                {
                    // Only accept the whitespace if we are going to consume the next token.
                    AcceptAndMoveNext();
                }

                Optional(SyntaxKind.QuestionMark); // Nullable

                if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.LeftBracket))
                {
                    // Only accept the whitespace if we are going to consume the next token.
                    AcceptAndMoveNext();
                }

                while (At(SyntaxKind.LeftBracket))
                {
                    Balance(BalancingModes.None);
                    Optional(SyntaxKind.RightBracket);
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
            Assert(SyntaxKind.LessThan);
            Balance(BalancingModes.None);
            Optional(SyntaxKind.GreaterThan);
        }

        private void UsingStatement(Block block)
        {
            Assert(SyntaxKind.LeftParenthesis);

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
            var block = new Block(CurrentToken, CurrentStart);

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
                if (!At(SyntaxKind.LeftBrace))
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsNotAllowed(
                            new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                            Language.GetSample(SyntaxKind.LeftBrace),
                            CurrentToken.Content));
                }

                // Parse the statement and then we're done
                Statement(block);
            }
        }

        private void UnconditionalBlock()
        {
            Assert(SyntaxKind.Keyword);
            var block = new Block(CurrentToken, CurrentStart);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
            ExpectCodeBlock(block);
        }

        private void FilterableCatchBlock()
        {
            Assert(CSharpKeyword.Catch);

            var block = new Block(CurrentToken, CurrentStart);

            // Accept "catch"
            AcceptAndMoveNext();
            AcceptWhile(IsValidStatementSpacingToken);

            // Parse the catch condition if present. If not present, let the C# compiler complain.
            if (AcceptCondition())
            {
                AcceptWhile(IsValidStatementSpacingToken);

                if (At(CSharpKeyword.When))
                {
                    // Accept "when".
                    AcceptAndMoveNext();
                    AcceptWhile(IsValidStatementSpacingToken);

                    // Parse the filter condition if present. If not present, let the C# compiler complain.
                    if (!AcceptCondition())
                    {
                        // Incomplete condition.
                        return;
                    }

                    AcceptWhile(IsValidStatementSpacingToken);
                }

                ExpectCodeBlock(block);
            }
        }

        private void ConditionalBlock(bool topLevel)
        {
            Assert(SyntaxKind.Keyword);
            var block = new Block(CurrentToken, CurrentStart);
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
            if (At(SyntaxKind.LeftParenthesis))
            {
                var complete = Balance(BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                if (!complete)
                {
                    AcceptUntil(SyntaxKind.NewLine);
                }
                else
                {
                    Optional(SyntaxKind.RightParenthesis);
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

            var type = CurrentToken.Kind;
            var loc = CurrentStart;

            // Both cases @: and @:: are triggered as markup, second colon in second case will be triggered as a plain text
            var isSingleLineMarkup = type == SyntaxKind.Transition &&
                (NextIs(SyntaxKind.Colon, SyntaxKind.DoubleColon));

            var isMarkup = isSingleLineMarkup ||
                type == SyntaxKind.LessThan ||
                (type == SyntaxKind.Transition && NextIs(SyntaxKind.LessThan));

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
                var nextToken = Lookahead(1);

                // MARKUP owns whitespace EXCEPT in DesignTimeMode.
                PutCurrentBack();

                // Put back the whitespace unless it precedes a '<text>' tag.
                if (nextToken != null &&
                    !string.Equals(nextToken.Content, SyntaxConstants.TextTagName, StringComparison.Ordinal))
                {
                    PutBack(lastWhitespace);
                }
                else
                {
                    // If it precedes a '<text>' tag, it should be accepted as code.
                    Accept(lastWhitespace);
                }
            }

            if (isMarkup)
            {
                if (type == SyntaxKind.Transition && !isSingleLineMarkup)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(
                            new SourceSpan(loc, contentLength: 1 /* @ */)));
                }

                // Markup block
                Output(SpanKindInternal.Code);
                if (Context.DesignTimeMode && CurrentToken != null &&
                    (CurrentToken.Kind == SyntaxKind.LessThan || CurrentToken.Kind == SyntaxKind.Transition))
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

        private void HandleStatement(Block block, SyntaxKind type)
        {
            switch (type)
            {
                case SyntaxKind.RazorCommentTransition:
                    Output(SpanKindInternal.Code);
                    RazorComment();
                    Statement(block);
                    break;
                case SyntaxKind.LeftBrace:
                    // Verbatim Block
                    block = block ?? new Block(Resources.BlockName_Code, CurrentStart);
                    AcceptAndMoveNext();
                    CodeBlock(block);
                    break;
                case SyntaxKind.Keyword:
                    // Keyword block
                    HandleKeyword(false, StandardStatement);
                    break;
                case SyntaxKind.Transition:
                    // Embedded Expression block
                    EmbeddedExpression();
                    break;
                case SyntaxKind.RightBrace:
                    // Possible end of Code Block, just run the continuation
                    break;
                case SyntaxKind.CSharpComment:
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
            Assert(SyntaxKind.Transition);
            var transition = CurrentToken;
            NextToken();

            if (At(SyntaxKind.Transition))
            {
                // Escaped "@"
                Output(SpanKindInternal.Code);

                // Output "@" as hidden span
                Accept(transition);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.Code);

                Assert(SyntaxKind.Transition);
                AcceptAndMoveNext();
                StandardStatement();
            }
            else
            {
                // Throw errors as necessary, but continue parsing
                if (At(SyntaxKind.LeftBrace))
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_UnexpectedNestedCodeBlock(
                            new SourceSpan(CurrentStart, contentLength: 1 /* { */)));
                }

                // @( or @foo - Nested expression, parse a child block
                PutCurrentBack();
                PutBack(transition);

                // Before exiting, add a marker span if necessary
                AddMarkerTokenIfNecessary();

                NestedBlock();
            }
        }

        private void StandardStatement()
        {
            while (!EndOfFile)
            {
                var bookmark = CurrentStart.AbsoluteIndex;
                var read = ReadWhile(token =>
                    token.Kind != SyntaxKind.Semicolon &&
                    token.Kind != SyntaxKind.RazorCommentTransition &&
                    token.Kind != SyntaxKind.Transition &&
                    token.Kind != SyntaxKind.LeftBrace &&
                    token.Kind != SyntaxKind.LeftParenthesis &&
                    token.Kind != SyntaxKind.LeftBracket &&
                    token.Kind != SyntaxKind.RightBrace);

                if (At(SyntaxKind.LeftBrace) ||
                    At(SyntaxKind.LeftParenthesis) ||
                    At(SyntaxKind.LeftBracket))
                {
                    Accept(read);
                    if (Balance(BalancingModes.AllowCommentsAndTemplates | BalancingModes.BacktrackOnFailure))
                    {
                        Optional(SyntaxKind.RightBrace);
                    }
                    else
                    {
                        // Recovery
                        AcceptUntil(SyntaxKind.LessThan, SyntaxKind.RightBrace);
                        return;
                    }
                }
                else if (At(SyntaxKind.Transition) && (NextIs(SyntaxKind.LessThan, SyntaxKind.Colon)))
                {
                    Accept(read);
                    Output(SpanKindInternal.Code);
                    Template();
                }
                else if (At(SyntaxKind.RazorCommentTransition))
                {
                    Accept(read);
                    RazorComment();
                }
                else if (At(SyntaxKind.Semicolon))
                {
                    Accept(read);
                    AcceptAndMoveNext();
                    return;
                }
                else if (At(SyntaxKind.RightBrace))
                {
                    Accept(read);
                    return;
                }
                else
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                    AcceptUntil(SyntaxKind.LessThan, SyntaxKind.LeftBrace, SyntaxKind.RightBrace);
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
            while (!EndOfFile && !At(SyntaxKind.RightBrace))
            {
                // Parse a statement, then return here
                Statement();
                EnsureCurrent();
            }

            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(block.Start, contentLength: 1 /* { OR } */), block.Name, "}", "{"));
            }
            else if (acceptTerminatingBrace)
            {
                Assert(SyntaxKind.RightBrace);
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                AcceptAndMoveNext();
            }
        }

        private void HandleKeyword(bool topLevel, Action fallback)
        {
            var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
            Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword && result.HasValue);
            if (_keywordParsers.TryGetValue(result.Value, out var handler))
            {
                handler(topLevel);
            }
            else
            {
                fallback();
            }
        }

        private IEnumerable<SyntaxToken> SkipToNextImportantToken()
        {
            while (!EndOfFile)
            {
                var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));
                if (At(SyntaxKind.RazorCommentTransition))
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
            return Enumerable.Empty<SyntaxToken>();
        }

        // Common code for Parsers, but FxCop REALLY doesn't like it in the base class.. moving it here for now.
        protected override void OutputSpanBeforeRazorComment()
        {
            AddMarkerTokenIfNecessary();
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
                        var currentDirective = CurrentToken.Content;
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_DirectiveMustAppearAtStartOfLine(
                                new SourceSpan(CurrentStart, currentDirective.Length), currentDirective));
                        break;
                    }
                }
            }
        }

        private void HandleDirective(DirectiveDescriptor descriptor)
        {
            AssertDirective(descriptor.Directive);

            var directiveErrorSink = new ErrorSink();
            var savedErrorSink = Context.ErrorSink;
            Context.ErrorSink = directiveErrorSink;

            var directiveChunkGenerator = new DirectiveChunkGenerator(descriptor);
            try
            {
                EnsureDirectiveIsAtStartOfLine();

                Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;
                Context.Builder.CurrentBlock.ChunkGenerator = directiveChunkGenerator;

                AcceptAndMoveNext();
                Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

                // Even if an error was logged do not bail out early. If a directive was used incorrectly it doesn't mean it can't be parsed.
                ValidateDirectiveUsage(descriptor);

                for (var i = 0; i < descriptor.Tokens.Count; i++)
                {
                    if (!At(SyntaxKind.Whitespace) &&
                        !At(SyntaxKind.NewLine) &&
                        !EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_DirectiveTokensMustBeSeparatedByWhitespace(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                        return;
                    }

                    var tokenDescriptor = descriptor.Tokens[i];
                    AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

                    if (tokenDescriptor.Kind == DirectiveTokenKind.Member ||
                        tokenDescriptor.Kind == DirectiveTokenKind.Namespace ||
                        tokenDescriptor.Kind == DirectiveTokenKind.Type)
                    {
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKindInternal.Code, AcceptedCharactersInternal.WhiteSpace);

                        if (EndOfFile || At(SyntaxKind.NewLine))
                        {
                            // Add a marker token to provide CSharp intellisense when we start typing the directive token.
                            AddMarkerTokenIfNecessary();
                            Span.ChunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                            Span.EditHandler = new DirectiveTokenEditHandler(Language.TokenizeString);
                            Output(SpanKindInternal.Code, AcceptedCharactersInternal.NonWhiteSpace);
                        }
                    }
                    else
                    {
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKindInternal.Markup, AcceptedCharactersInternal.WhiteSpace);
                    }

                    if (tokenDescriptor.Optional && (EndOfFile || At(SyntaxKind.NewLine)))
                    {
                        break;
                    }
                    else if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                                new SourceSpan(CurrentStart, contentLength: 1),
                                descriptor.Directive,
                                tokenDescriptor.Kind.ToString().ToLowerInvariant()));
                        return;
                    }

                    switch (tokenDescriptor.Kind)
                    {
                        case DirectiveTokenKind.Type:
                            if (!NamespaceOrTypeName())
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsTypeName(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));

                                return;
                            }
                            break;

                        case DirectiveTokenKind.Namespace:
                            if (!QualifiedIdentifier(out var identifierLength))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsNamespace(
                                        new SourceSpan(CurrentStart, identifierLength), descriptor.Directive));

                                return;
                            }
                            break;

                        case DirectiveTokenKind.Member:
                            if (At(SyntaxKind.Identifier))
                            {
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                return;
                            }
                            break;

                        case DirectiveTokenKind.String:
                            if (At(SyntaxKind.StringLiteral) && !CurrentToken.ContainsDiagnostics)
                            {
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsQuotedStringLiteral(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
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
                        Output(SpanKindInternal.None, AcceptedCharactersInternal.WhiteSpace);

                        Optional(SyntaxKind.Semicolon);
                        Span.ChunkGenerator = SpanChunkGenerator.Null;
                        Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.WhiteSpace);

                        AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

                        if (At(SyntaxKind.NewLine))
                        {
                            AcceptAndMoveNext();
                        }
                        else if (!EndOfFile)
                        {
                            Context.ErrorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_UnexpectedDirectiveLiteral(
                                    new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                    descriptor.Directive,
                                    Resources.ErrorComponent_Newline));
                        }

                        Span.ChunkGenerator = SpanChunkGenerator.Null;

                        // This should contain the optional whitespace after the optional semicolon and the new line.
                        // Output as Markup as we want intellisense here.
                        Output(SpanKindInternal.Markup, AcceptedCharactersInternal.WhiteSpace);
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
                            Balance(BalancingModes.NoErrorOnFailure, SyntaxKind.LeftBrace, SyntaxKind.RightBrace, startingBraceLocation);
                            Span.ChunkGenerator = new StatementChunkGenerator();
                            var existingEditHandler = Span.EditHandler;
                            Span.EditHandler = new CodeBlockEditHandler(Language.TokenizeString);

                            AddMarkerTokenIfNecessary();

                            Output(SpanKindInternal.Code);

                            Span.EditHandler = existingEditHandler;
                        });
                        break;
                }
            }
            finally
            {
                if (directiveErrorSink.Errors.Count > 0)
                {
                    directiveChunkGenerator.Diagnostics.AddRange(directiveErrorSink.Errors);
                }

                Context.ErrorSink = savedErrorSink;
            }
        }


        private void ValidateDirectiveUsage(DirectiveDescriptor descriptor)
        {
            if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
            {
                if (Context.SeenDirectives.Contains(descriptor.Directive))
                {
                    // There will always be at least 1 child because of the `@` transition.
                    var directiveStart = Context.Builder.CurrentBlock.Children.First().Start;
                    var errorLength = /* @ */ 1 + descriptor.Directive.Length;
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                            new SourceSpan(directiveStart, errorLength), descriptor.Directive));

                    return;
                }
            }
        }

        private void ParseDirectiveBlock(DirectiveDescriptor descriptor, Action<SourceLocation> parseChildren)
        {
            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                        new SourceSpan(CurrentStart, contentLength: 1 /* { */), descriptor.Directive, "{"));
            }
            else if (!At(SyntaxKind.LeftBrace))
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_UnexpectedDirectiveLiteral(
                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive, "{"));
            }
            else
            {
                var editHandler = new AutoCompleteEditHandler(Language.TokenizeString, autoCompleteAtEndOfSpan: true);
                Span.EditHandler = editHandler;
                var startingBraceLocation = CurrentStart;
                Accept(CurrentToken);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

                parseChildren(startingBraceLocation);

                Span.ChunkGenerator = SpanChunkGenerator.Null;
                if (!Optional(SyntaxKind.RightBrace))
                {
                    editHandler.AutoCompleteString = "}";
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                            new SourceSpan(startingBraceLocation, contentLength: 1 /* } */), descriptor.Directive, "}", "{"));
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
            RazorDiagnostic duplicateDiagnostic = null;
            if (Context.SeenDirectives.Contains(SyntaxConstants.CSharp.TagHelperPrefixKeyword))
            {
                // There wil always be at least 1 child because of the `@` transition.
                var directiveStart = Context.Builder.CurrentBlock.Children.First().Start;
                var errorLength = /* @ */ 1 + SyntaxConstants.CSharp.TagHelperPrefixKeyword.Length;
                duplicateDiagnostic = RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                    new SourceSpan(directiveStart, errorLength),
                    SyntaxConstants.CSharp.TagHelperPrefixKeyword);
            }

            TagHelperDirective(
                SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                (prefix, errors) =>
                {
                    if (duplicateDiagnostic != null)
                    {
                        errors.Add(duplicateDiagnostic);
                    }

                    var parsedDirective = ParseDirective(prefix, Span.Start, TagHelperDirectiveType.TagHelperPrefix, errors);

                    return new TagHelperPrefixDirectiveChunkGenerator(
                        prefix,
                        parsedDirective.DirectiveText,
                        errors);
                });
        }

        // Internal for testing.
        internal void ValidateTagHelperPrefix(
            string prefix,
            SourceLocation directiveLocation,
            List<RazorDiagnostic> diagnostics)
        {
            foreach (var character in prefix)
            {
                // Prefixes are correlated with tag names, tag names cannot have whitespace.
                if (char.IsWhiteSpace(character) || InvalidNonWhitespaceNameCharacters.Contains(character))
                {
                    diagnostics.Add(
                        RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                            new SourceSpan(directiveLocation, prefix.Length),
                            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                            character,
                            prefix));

                    return;
                }
            }
        }

        private ParsedDirective ParseDirective(
            string directiveText,
            SourceLocation directiveLocation,
            TagHelperDirectiveType directiveType,
            List<RazorDiagnostic> errors)
        {
            var offset = 0;
            directiveText = directiveText.Trim();
            if (directiveText.Length >= 2 &&
                directiveText.StartsWith("\"", StringComparison.Ordinal) &&
                directiveText.EndsWith("\"", StringComparison.Ordinal))
            {
                directiveText = directiveText.Substring(1, directiveText.Length - 2);
                if (string.IsNullOrEmpty(directiveText))
                {
                    offset = 1;
                }
            }

            // If this is the "string literal" form of a directive, we'll need to postprocess the location
            // and content.
            //
            // Ex: @addTagHelper "*, Microsoft.AspNetCore.CoolLibrary"
            //                    ^                                 ^
            //                  Start                              End
            if (Span.Tokens.Count == 1 && (Span.Tokens[0] as SyntaxToken)?.Kind == SyntaxKind.StringLiteral)
            {
                offset += Span.Tokens[0].Content.IndexOf(directiveText, StringComparison.Ordinal);

                // This is safe because inside one of these directives all of the text needs to be on the
                // same line.
                var original = directiveLocation;
                directiveLocation = new SourceLocation(
                    original.FilePath,
                    original.AbsoluteIndex + offset,
                    original.LineIndex,
                    original.CharacterIndex + offset);
            }

            var parsedDirective = new ParsedDirective()
            {
                DirectiveText = directiveText
            };

            if (directiveType == TagHelperDirectiveType.TagHelperPrefix)
            {
                ValidateTagHelperPrefix(parsedDirective.DirectiveText, directiveLocation, errors);

                return parsedDirective;
            }

            return ParseAddOrRemoveDirective(parsedDirective, directiveLocation, errors);
        }

        // Internal for testing.
        internal ParsedDirective ParseAddOrRemoveDirective(ParsedDirective directive, SourceLocation directiveLocation, List<RazorDiagnostic> errors)
        {
            var text = directive.DirectiveText;
            var lookupStrings = text?.Split(new[] { ',' });

            // Ensure that we have valid lookupStrings to work with. The valid format is "typeName, assemblyName"
            if (lookupStrings == null ||
                lookupStrings.Any(string.IsNullOrWhiteSpace) ||
                lookupStrings.Length != 2 ||
                text.StartsWith("'") ||
                text.EndsWith("'"))
            {
                errors.Add(
                    RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                        new SourceSpan(directiveLocation, Math.Max(text.Length, 1)), text));

                return directive;
            }

            var trimmedAssemblyName = lookupStrings[1].Trim();

            // + 1 is for the comma separator in the lookup text.
            var assemblyNameIndex =
                lookupStrings[0].Length + 1 + lookupStrings[1].IndexOf(trimmedAssemblyName, StringComparison.Ordinal);
            var assemblyNamePrefix = directive.DirectiveText.Substring(0, assemblyNameIndex);

            directive.TypePattern = lookupStrings[0].Trim();
            directive.AssemblyName = trimmedAssemblyName;

            return directive;
        }

        protected virtual void AddTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                (lookupText, errors) =>
                {
                    var parsedDirective = ParseDirective(lookupText, Span.Start, TagHelperDirectiveType.AddTagHelper, errors);

                    return new AddTagHelperChunkGenerator(
                        lookupText,
                        parsedDirective.DirectiveText,
                        parsedDirective.TypePattern,
                        parsedDirective.AssemblyName,
                        errors);
                });
        }

        protected virtual void RemoveTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.RemoveTagHelperKeyword,
                (lookupText, errors) =>
                {
                    var parsedDirective = ParseDirective(lookupText, Span.Start, TagHelperDirectiveType.RemoveTagHelper, errors);

                    return new RemoveTagHelperChunkGenerator(
                        lookupText,
                        parsedDirective.DirectiveText,
                        parsedDirective.TypePattern,
                        parsedDirective.AssemblyName,
                        errors);
                });
        }

        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.Identifier || CurrentToken.Kind == SyntaxKind.Keyword);
            Debug.Assert(string.Equals(CurrentToken.Content, directive, StringComparison.Ordinal));
        }

        private void TagHelperDirective(string keyword, Func<string, List<RazorDiagnostic>, ISpanChunkGenerator> chunkGeneratorFactory)
        {
            AssertDirective(keyword);

            var savedErrorSink = Context.ErrorSink;
            var directiveErrorSink = new ErrorSink();
            Context.ErrorSink = directiveErrorSink;

            string directiveValue = null;
            try
            {
                EnsureDirectiveIsAtStartOfLine();

                var keywordStartLocation = CurrentStart;

                // Accept the directive name
                AcceptAndMoveNext();

                // Set the block type
                Context.Builder.CurrentBlock.Type = BlockKindInternal.Directive;

                var keywordLength = Span.End.AbsoluteIndex - Span.Start.AbsoluteIndex;

                var foundWhitespace = At(SyntaxKind.Whitespace);

                // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
                // to the document.  We can't accept it.
                var acceptedCharacters = foundWhitespace ? AcceptedCharactersInternal.None : AcceptedCharactersInternal.AnyExceptNewline;
                Output(SpanKindInternal.MetaCode, acceptedCharacters);

                AcceptWhile(SyntaxKind.Whitespace);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Output(SpanKindInternal.Markup, acceptedCharacters);

                if (EndOfFile || At(SyntaxKind.NewLine))
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_DirectiveMustHaveValue(
                            new SourceSpan(keywordStartLocation, keywordLength), keyword));

                    directiveValue = string.Empty;
                }
                else
                {
                    // Need to grab the current location before we accept until the end of the line.
                    var startLocation = CurrentStart;

                    // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                    // etc.
                    AcceptUntil(SyntaxKind.NewLine);

                    // Pull out the value and remove whitespaces and optional quotes
                    var rawValue = string.Concat(Span.Tokens.Select(s => s.Content)).Trim();

                    var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                    var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                    if (startsWithQuote != endsWithQuote)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_IncompleteQuotesAroundDirective(
                                new SourceSpan(startLocation, rawValue.Length), keyword));
                    }

                    directiveValue = rawValue;
                }
            }
            finally
            {
                Span.ChunkGenerator = chunkGeneratorFactory(directiveValue, directiveErrorSink.Errors.ToList());
                Context.ErrorSink = savedErrorSink;
            }

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

            public Block(SyntaxToken token, SourceLocation start)
                : this(GetName(token), start)
            {
            }

            public string Name { get; set; }
            public SourceLocation Start { get; set; }

            private static string GetName(SyntaxToken token)
            {
                var result = CSharpTokenizer.GetTokenKeyword(token);
                if (result.HasValue && token.Kind == SyntaxKind.Keyword)
                {
                    return CSharpLanguageCharacteristics.GetKeyword(result.Value);
                }
                return token.Content;
            }
        }

        internal class ParsedDirective
        {
            public string DirectiveText { get; set; }

            public string AssemblyName { get; set; }

            public string TypePattern { get; set; }
        }
    }
}
