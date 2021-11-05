// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class CSharpCodeParser : TokenizerBackedParser<CSharpTokenizer>
{
    private static readonly HashSet<char> InvalidNonWhitespaceNameCharacters = new HashSet<char>(new[]
    {
            '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*'
        });

    private static readonly Func<SyntaxToken, bool> IsValidStatementSpacingToken =
        IsSpacingTokenIncludingNewLinesAndComments;

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
            "where"
        };

    private readonly ISet<string> CurrentKeywords = new HashSet<string>(DefaultKeywords);

    private readonly Dictionary<CSharpKeyword, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>> _keywordParserMap = new Dictionary<CSharpKeyword, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>>();
    private readonly Dictionary<string, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>> _directiveParserMap = new Dictionary<string, Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax>>(StringComparer.Ordinal);

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
        SetupKeywordParsers();
        SetupExpressionParsers();
        SetupDirectiveParsers(directives);
    }

    public HtmlMarkupParser HtmlParser { get; set; }

    protected internal ISet<string> Keywords { get; private set; }

    public bool IsNested { get; set; }

    public CSharpCodeBlockSyntax ParseBlock()
    {
        if (Context == null)
        {
            throw new InvalidOperationException(Resources.Parser_Context_Not_Set);
        }

        if (EndOfFile)
        {
            // Nothing to parse.
            return null;
        }

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        using (PushSpanContextConfig(DefaultSpanContextConfig))
        {
            var builder = pooledResult.Builder;
            try
            {
                NextToken();

                var precedingWhitespace = ReadWhile(IsSpacingTokenIncludingNewLinesAndComments);

                // We are usually called when the other parser sees a transition '@'. Look for it.
                SyntaxToken transitionToken = null;
                if (At(SyntaxKind.StringLiteral) &&
                    CurrentToken.Content.Length > 0 &&
                    CurrentToken.Content[0] == SyntaxConstants.TransitionCharacter)
                {
                    var split = Language.SplitToken(CurrentToken, 1, SyntaxKind.Transition);
                    transitionToken = split.Item1;

                    // Back up to the end of the transition
                    Context.Source.Position -= split.Item2.Content.Length;
                    NextToken();
                }
                else if (At(SyntaxKind.Transition))
                {
                    transitionToken = EatCurrentToken();
                }

                if (transitionToken == null)
                {
                    transitionToken = SyntaxFactory.MissingToken(SyntaxKind.Transition);
                }

                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                var transition = GetNodeWithSpanContext(SyntaxFactory.CSharpTransition(transitionToken));

                if (At(SyntaxKind.LeftBrace))
                {
                    // This is a statement. We want to preserve preceding whitespace in the output.
                    Accept(precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    var statementBody = ParseStatementBody();
                    var statement = SyntaxFactory.CSharpStatement(transition, statementBody);
                    builder.Add(statement);
                }
                else if (At(SyntaxKind.LeftParenthesis))
                {
                    // This is an explicit expression. We want to preserve preceding whitespace in the output.
                    Accept(precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    var expressionBody = ParseExplicitExpressionBody();
                    var expression = SyntaxFactory.CSharpExplicitExpression(transition, expressionBody);
                    builder.Add(expression);
                }
                else if (At(SyntaxKind.Identifier))
                {
                    if (!TryParseDirective(builder, precedingWhitespace, transition, CurrentToken.Content))
                    {
                        // Not a directive.
                        // This is an implicit expression. We want to preserve preceding whitespace in the output.
                        Accept(precedingWhitespace);
                        builder.Add(OutputTokensAsStatementLiteral());

                        if (string.Equals(
                            CurrentToken.Content,
                            SyntaxConstants.CSharp.HelperKeyword,
                            StringComparison.Ordinal))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateParsing_HelperDirectiveNotAvailable(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length));
                            CurrentToken.SetDiagnostics(new[] { diagnostic });
                            Context.ErrorSink.OnError(diagnostic);
                        }

                        var implicitExpressionBody = ParseImplicitExpressionBody();
                        var implicitExpression = SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody);
                        builder.Add(implicitExpression);
                    }
                }
                else if (At(SyntaxKind.Keyword))
                {
                    if (!TryParseDirective(builder, precedingWhitespace, transition, CurrentToken.Content) &&
                        !TryParseKeyword(builder, precedingWhitespace, transition))
                    {
                        // Not a directive or keyword.
                        // This is an implicit expression. We want to preserve preceding whitespace in the output.
                        Accept(precedingWhitespace);
                        builder.Add(OutputTokensAsStatementLiteral());

                        // Not a directive or a special keyword. Just parse as an implicit expression.
                        var implicitExpressionBody = ParseImplicitExpressionBody();
                        var implicitExpression = SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody);
                        builder.Add(implicitExpression);
                    }

                    builder.Add(OutputTokensAsStatementLiteral());
                }
                else
                {
                    // Invalid character after transition.
                    // Preserve the preceding whitespace in the output
                    Accept(precedingWhitespace);
                    builder.Add(OutputTokensAsStatementLiteral());

                    SpanContext.ChunkGenerator = new ExpressionChunkGenerator();
                    SpanContext.EditHandler = new ImplicitExpressionEditHandler(
                        LanguageTokenizeString,
                        CurrentKeywords,
                        acceptTrailingDot: IsNested)
                    {
                        AcceptedCharacters = AcceptedCharactersInternal.NonWhitespace
                    };

                    AcceptMarkerTokenIfNecessary();
                    var expressionLiteral = SyntaxFactory.CSharpCodeBlock(OutputTokensAsExpressionLiteral());
                    var expressionBody = SyntaxFactory.CSharpImplicitExpressionBody(expressionLiteral);
                    var expressionBlock = SyntaxFactory.CSharpImplicitExpression(transition, expressionBody);
                    builder.Add(expressionBlock);

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

                Debug.Assert(TokenBuilder.Count == 0, "We should not have any tokens left.");

                var codeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
                return codeBlock;
            }
            finally
            {
                // Always put current character back in the buffer for the next parser.
                PutCurrentBack();
            }
        }
    }

    private CSharpExplicitExpressionBodySyntax ParseExplicitExpressionBody()
    {
        var block = new Block(Resources.BlockName_ExplicitExpression, CurrentStart);
        Assert(SyntaxKind.LeftParenthesis);
        var leftParenToken = EatCurrentToken();
        var leftParen = OutputAsMetaCode(leftParenToken);

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var expressionBuilder = pooledResult.Builder;
            using (PushSpanContextConfig(ExplicitExpressionSpanContextConfig))
            {
                var success = Balance(
                    expressionBuilder,
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
                AcceptMarkerTokenIfNecessary();
                expressionBuilder.Add(OutputTokensAsExpressionLiteral());
            }

            var expressionBlock = SyntaxFactory.CSharpCodeBlock(expressionBuilder.ToList());

            RazorMetaCodeSyntax rightParen = null;
            if (At(SyntaxKind.RightParenthesis))
            {
                rightParen = OutputAsMetaCode(EatCurrentToken());
            }
            else
            {
                var missingToken = SyntaxFactory.MissingToken(SyntaxKind.RightParenthesis);
                rightParen = OutputAsMetaCode(missingToken, SpanContext.EditHandler.AcceptedCharacters);
            }
            if (!EndOfFile)
            {
                PutCurrentBack();
            }

            return SyntaxFactory.CSharpExplicitExpressionBody(leftParen, expressionBlock, rightParen);
        }
    }

    private CSharpImplicitExpressionBodySyntax ParseImplicitExpressionBody(bool async = false)
    {
        var accepted = AcceptedCharactersInternal.NonWhitespace;
        if (async)
        {
            // Async implicit expressions include the "await" keyword and therefore need to allow spaces to
            // separate the "await" and the following code.
            accepted = AcceptedCharactersInternal.AnyExceptNewline;
        }

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var expressionBuilder = pooledResult.Builder;
            ParseImplicitExpression(expressionBuilder, accepted);
            var codeBlock = SyntaxFactory.CSharpCodeBlock(expressionBuilder.ToList());
            return SyntaxFactory.CSharpImplicitExpressionBody(codeBlock);
        }
    }

    private void ParseImplicitExpression(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal acceptedCharacters)
    {
        using (PushSpanContextConfig(spanContext =>
        {
            spanContext.EditHandler = new ImplicitExpressionEditHandler(
                LanguageTokenizeString,
                Keywords,
                acceptTrailingDot: IsNested);
            spanContext.EditHandler.AcceptedCharacters = acceptedCharacters;
            spanContext.ChunkGenerator = new ExpressionChunkGenerator();
        }))
        {
            do
            {
                if (AtIdentifier(allowKeywords: true))
                {
                    AcceptAndMoveNext();
                }
            }
            while (ParseMethodCallOrArrayIndex(builder, acceptedCharacters));

            PutCurrentBack();
            builder.Add(OutputTokensAsExpressionLiteral());
        }
    }

    private bool ParseMethodCallOrArrayIndex(in SyntaxListBuilder<RazorSyntaxNode> builder, AcceptedCharactersInternal acceptedCharacters)
    {
        if (!EndOfFile)
        {
            if (CurrentToken.Kind == SyntaxKind.LeftParenthesis ||
                CurrentToken.Kind == SyntaxKind.LeftBracket)
            {
                // If we end within "(", whitespace is fine
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;

                SyntaxKind right;
                bool success;

                using (PushSpanContextConfig((spanContext, prev) =>
                {
                    prev(spanContext);
                    spanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                }))
                {
                    right = Language.FlipBracket(CurrentToken.Kind);
                    success = Balance(builder, BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
                }

                if (!success)
                {
                    AcceptUntil(SyntaxKind.LessThan);
                }
                if (At(right))
                {
                    AcceptAndMoveNext();

                    // At the ending brace, restore the initial accepted characters.
                    SpanContext.EditHandler.AcceptedCharacters = acceptedCharacters;
                }
                return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
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
                        return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
                    }
                }
            }
            else if (At(SyntaxKind.Not) && Context.FeatureFlags.AllowNullableForgivenessOperator)
            {
                // C# 8.0 Null forgiveness Operator

                var next = Lookahead(count: 1);
                if (next == null)
                {
                    // Null forgiveness operator at the end of the file, don't include it in the expression.
                    // We don't allow trailing null forgiveness operators to avoid breaking scenarios such as:
                    //
                    // <p>Hello @Person! Good day!</p>
                    return false;
                }

                if (next.Kind == SyntaxKind.Dot)
                {
                    var nextNext = Lookahead(count: 2);
                    if (nextNext == null)
                    {
                        // End of file after the dot (!.EOF)
                        return false;
                    }

                    if (nextNext.Kind == SyntaxKind.Identifier || nextNext.Kind == SyntaxKind.Keyword)
                    {
                        // Accept null forgiveness operator followed by a dot (!.)
                        AcceptAndMoveNext();

                        // Accept the dot
                        AcceptAndMoveNext();
                        return true;
                    }

                    // We're in an odd situation where the user is attempting to use a null-forgiven implicit expression at the
                    // end of a sentence, i.e.
                    //
                    // <p>@Person!.</p>
                    //
                    // We don't allow trailing null forgiveness operators so don't include it in the implicit expression.
                    return false;
                }
                else if (next.Kind == SyntaxKind.QuestionMark)
                {
                    // We're at the ! for a null forgiveness + null conditional operator (!?).
                    AcceptAndMoveNext();

                    return true;
                }
                else if (next.Kind == SyntaxKind.LeftBracket || next.Kind == SyntaxKind.LeftParenthesis)
                {
                    // We're at the ! for a null forgiveness bracket or parenthesis operator (![).
                    AcceptAndMoveNext();

                    // Accept the [ or ( and any content inside (it will attempt to balance).
                    return ParseMethodCallOrArrayIndex(builder, acceptedCharacters);
                }

                return false;
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

    private CSharpStatementBodySyntax ParseStatementBody(Block block = null)
    {
        Assert(SyntaxKind.LeftBrace);
        block = block ?? new Block(Resources.BlockName_Code, CurrentStart);
        var leftBrace = OutputAsMetaCode(EatExpectedToken(SyntaxKind.LeftBrace));
        CSharpCodeBlockSyntax codeBlock = null;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var builder = pooledResult.Builder;
            // Set up auto-complete and parse the code block
            var editHandler = new AutoCompleteEditHandler(LanguageTokenizeString);
            SpanContext.EditHandler = editHandler;
            ParseCodeBlock(builder, block);

            if (EndOfFile)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(block.Start, contentLength: 1 /* { OR } */), block.Name, "}", "{"));
            }

            EnsureCurrent();
            SpanContext.ChunkGenerator = new StatementChunkGenerator();
            AcceptMarkerTokenIfNecessary();
            if (!At(SyntaxKind.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
            }
            builder.Add(OutputTokensAsStatementLiteral());

            codeBlock = SyntaxFactory.CSharpCodeBlock(builder.ToList());
        }

        RazorMetaCodeSyntax rightBrace = null;
        if (At(SyntaxKind.RightBrace))
        {
            rightBrace = OutputAsMetaCode(EatCurrentToken());
        }
        else
        {
            rightBrace = OutputAsMetaCode(
                SyntaxFactory.MissingToken(SyntaxKind.RightBrace),
                SpanContext.EditHandler.AcceptedCharacters);
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

        return SyntaxFactory.CSharpStatementBody(leftBrace, codeBlock, rightBrace);
    }

    private void ParseCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        EnsureCurrent();
        while (!EndOfFile && !At(SyntaxKind.RightBrace))
        {
            // Parse a statement, then return here
            ParseStatement(builder, block: block);
            EnsureCurrent();
        }
    }

    private void ParseStatement(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
        // Accept whitespace but always keep the last whitespace node so we can put it back if necessary
        var lastWhitespace = AcceptWhitespaceInLines();
        if (EndOfFile)
        {
            if (lastWhitespace != null)
            {
                Accept(lastWhitespace);
            }

            builder.Add(OutputTokensAsStatementLiteral());
            return;
        }

        var kind = CurrentToken.Kind;
        var location = CurrentStart;

        // Both cases @: and @:: are triggered as markup, second colon in second case will be triggered as a plain text
        var isSingleLineMarkup = kind == SyntaxKind.Transition &&
            (NextIs(SyntaxKind.Colon, SyntaxKind.DoubleColon));

        var isMarkup = isSingleLineMarkup ||
            kind == SyntaxKind.LessThan ||
            (kind == SyntaxKind.Transition && NextIs(SyntaxKind.LessThan));

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
            if (kind == SyntaxKind.Transition && !isSingleLineMarkup)
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(
                        new SourceSpan(location, contentLength: 1 /* @ */)));
            }

            // Markup block
            builder.Add(OutputTokensAsStatementLiteral());
            if (Context.DesignTimeMode && CurrentToken != null &&
                (CurrentToken.Kind == SyntaxKind.LessThan || CurrentToken.Kind == SyntaxKind.Transition))
            {
                PutCurrentBack();
            }
            OtherParserBlock(builder);
        }
        else
        {
            // What kind of statement is this?
            switch (kind)
            {
                case SyntaxKind.RazorCommentTransition:
                    AcceptMarkerTokenIfNecessary();
                    builder.Add(OutputTokensAsStatementLiteral());
                    var comment = ParseRazorComment();
                    builder.Add(comment);
                    ParseStatement(builder, block);
                    break;
                case SyntaxKind.LeftBrace:
                    // Verbatim Block
                    AcceptAndMoveNext();
                    ParseCodeBlock(builder, block);

                    // ParseCodeBlock is responsible for parsing the insides of a code block (non-inclusive of braces).
                    // Therefore, there's one of two cases after parsing:
                    //  1. We've hit the End of File (incomplete parse block).
                    //  2. It's a complete parse block and we're at a right brace.

                    if (EndOfFile)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(block.Start, contentLength: 1 /* { OR } */), block.Name, "}", "{"));
                    }
                    else
                    {
                        Assert(SyntaxKind.RightBrace);
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                        AcceptAndMoveNext();
                    }
                    break;
                case SyntaxKind.Keyword:
                    if (!TryParseKeyword(builder, whitespace: null, transition: null))
                    {
                        ParseStandardStatement(builder);
                    }
                    break;
                case SyntaxKind.Transition:
                    // Embedded Expression block
                    ParseEmbeddedExpression(builder);
                    break;
                case SyntaxKind.RightBrace:
                    // Possible end of Code Block, just run the continuation
                    break;
                case SyntaxKind.CSharpComment:
                    Accept(CurrentToken);
                    NextToken();
                    break;
                default:
                    // Other statement
                    ParseStandardStatement(builder);
                    break;
            }
        }
    }

    private void ParseEmbeddedExpression(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // First, verify the type of the block
        Assert(SyntaxKind.Transition);
        var transition = CurrentToken;
        NextToken();

        if (At(SyntaxKind.Transition))
        {
            // Escaped "@"
            builder.Add(OutputTokensAsStatementLiteral());

            // Output "@" as hidden span
            Accept(transition);
            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            builder.Add(OutputTokensAsEphemeralLiteral());

            Assert(SyntaxKind.Transition);
            AcceptAndMoveNext();
            ParseStandardStatement(builder);
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
            AcceptMarkerTokenIfNecessary();
            builder.Add(OutputTokensAsStatementLiteral());

            var nestedBlock = ParseNestedBlock();
            builder.Add(nestedBlock);
        }
    }

    private RazorSyntaxNode ParseNestedBlock()
    {
        var wasNested = IsNested;
        IsNested = true;

        RazorSyntaxNode nestedBlock;
        using (PushSpanContextConfig())
        {
            nestedBlock = ParseBlock();
        }

        InitializeContext(SpanContext);
        IsNested = wasNested;
        NextToken();

        return nestedBlock;
    }

    private void ParseStandardStatement(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        while (!EndOfFile)
        {
            var bookmark = CurrentStart.AbsoluteIndex;
            var read = ReadWhile(static token =>
                token.Kind != SyntaxKind.Semicolon &&
                token.Kind != SyntaxKind.RazorCommentTransition &&
                token.Kind != SyntaxKind.Transition &&
                token.Kind != SyntaxKind.LeftBrace &&
                token.Kind != SyntaxKind.LeftParenthesis &&
                token.Kind != SyntaxKind.LeftBracket &&
                token.Kind != SyntaxKind.RightBrace);

            if ((!Context.FeatureFlags.AllowRazorInAllCodeBlocks && At(SyntaxKind.LeftBrace)) ||
                At(SyntaxKind.LeftParenthesis) ||
                At(SyntaxKind.LeftBracket))
            {
                Accept(read);
                if (Balance(builder, BalancingModes.AllowCommentsAndTemplates | BalancingModes.BacktrackOnFailure))
                {
                    TryAccept(SyntaxKind.RightBrace);
                }
                else
                {
                    // Recovery
                    AcceptUntil(SyntaxKind.LessThan, SyntaxKind.RightBrace);
                    return;
                }
            }
            else if (Context.FeatureFlags.AllowRazorInAllCodeBlocks && At(SyntaxKind.LeftBrace))
            {
                Accept(read);
                return;
            }
            else if (At(SyntaxKind.Transition) && (NextIs(SyntaxKind.LessThan, SyntaxKind.Colon)))
            {
                Accept(read);
                builder.Add(OutputTokensAsStatementLiteral());
                ParseTemplate(builder);
            }
            else if (At(SyntaxKind.RazorCommentTransition))
            {
                Accept(read);
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputTokensAsStatementLiteral());
                builder.Add(ParseRazorComment());
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

    private void ParseTemplate(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (Context.InTemplateContext)
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_InlineMarkupBlocksCannotBeNested(
                    new SourceSpan(CurrentStart, contentLength: 1 /* @ */)));
        }
        if (SpanContext.ChunkGenerator is ExpressionChunkGenerator)
        {
            builder.Add(OutputTokensAsExpressionLiteral());
        }
        else
        {
            builder.Add(OutputTokensAsStatementLiteral());
        }

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var templateBuilder = pooledResult.Builder;
            Context.InTemplateContext = true;
            PutCurrentBack();
            OtherParserBlock(templateBuilder);

            var template = SyntaxFactory.CSharpTemplateBlock(templateBuilder.ToList());
            builder.Add(template);

            Context.InTemplateContext = false;
        }
    }

    protected bool TryParseDirective(in SyntaxListBuilder<RazorSyntaxNode> builder, IReadOnlyList<SyntaxToken> whitespace, CSharpTransitionSyntax transition, string directive)
    {
        if (_directiveParserMap.TryGetValue(directive, out var handler))
        {
            // This is a directive. We don't want to generate the preceding whitespace in the output.
            Accept(whitespace);
            builder.Add(OutputTokensAsEphemeralLiteral());

            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            handler(builder, transition);
            return true;
        }

        return false;
    }

    private void SetupDirectiveParsers(IEnumerable<DirectiveDescriptor> directiveDescriptors)
    {
        foreach (var directiveDescriptor in directiveDescriptors)
        {
            CurrentKeywords.Add(directiveDescriptor.Directive);
            MapDirectives((builder, transition) => ParseExtensibleDirective(builder, transition, directiveDescriptor), directiveDescriptor.Directive);
        }

        MapDirectives(ParseTagHelperPrefixDirective, SyntaxConstants.CSharp.TagHelperPrefixKeyword);
        MapDirectives(ParseAddTagHelperDirective, SyntaxConstants.CSharp.AddTagHelperKeyword);
        MapDirectives(ParseRemoveTagHelperDirective, SyntaxConstants.CSharp.RemoveTagHelperKeyword);

        // If there wasn't any extensible directives relating to the reserved directives then map them.
        if (!_directiveParserMap.ContainsKey("class"))
        {
            MapDirectives(ParseReservedDirective, "class");
        }

        if (!_directiveParserMap.ContainsKey("namespace"))
        {
            MapDirectives(ParseReservedDirective, "namespace");
        }
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

    // Internal for testing.
    internal void MapDirectives(Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax> handler, params string[] directives)
    {
        foreach (var directive in directives)
        {
            if (_directiveParserMap.ContainsKey(directive))
            {
                // It is possible for the list to contain duplicates in cases when the project is misconfigured.
                // In those cases, we shouldn't register multiple handlers per keyword.
                continue;
            }

            _directiveParserMap.Add(directive, (builder, transition) =>
            {
                handler(builder, transition);
                Context.SeenDirectives.Add(directive);
            });

            Keywords.Add(directive);
        }
    }

    private void ParseTagHelperPrefixDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        RazorDiagnostic duplicateDiagnostic = null;
        if (Context.SeenDirectives.Contains(SyntaxConstants.CSharp.TagHelperPrefixKeyword))
        {
            var directiveStart = CurrentStart;
            if (transition != null)
            {
                // Start the error from the Transition '@'.
                directiveStart = new SourceLocation(
                    directiveStart.FilePath,
                    directiveStart.AbsoluteIndex - 1,
                    directiveStart.LineIndex,
                    directiveStart.CharacterIndex - 1);
            }
            var errorLength = /* @ */ 1 + SyntaxConstants.CSharp.TagHelperPrefixKeyword.Length;
            duplicateDiagnostic = RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                new SourceSpan(directiveStart, errorLength),
                SyntaxConstants.CSharp.TagHelperPrefixKeyword);
        }

        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
            (prefix, errors, startLocation) =>
            {
                if (duplicateDiagnostic != null)
                {
                    errors.Add(duplicateDiagnostic);
                }

                var parsedDirective = ParseDirective(prefix, startLocation, TagHelperDirectiveType.TagHelperPrefix, errors);

                return new TagHelperPrefixDirectiveChunkGenerator(
                    prefix,
                    parsedDirective.DirectiveText,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    private void ParseAddTagHelperDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.AddTagHelperKeyword,
            (lookupText, errors, startLocation) =>
            {
                var parsedDirective = ParseDirective(lookupText, startLocation, TagHelperDirectiveType.AddTagHelper, errors);

                return new AddTagHelperChunkGenerator(
                    lookupText,
                    parsedDirective.DirectiveText,
                    parsedDirective.TypePattern,
                    parsedDirective.AssemblyName,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    private void ParseRemoveTagHelperDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        var directiveBody = ParseTagHelperDirective(
            SyntaxConstants.CSharp.RemoveTagHelperKeyword,
            (lookupText, errors, startLocation) =>
            {
                var parsedDirective = ParseDirective(lookupText, startLocation, TagHelperDirectiveType.RemoveTagHelper, errors);

                return new RemoveTagHelperChunkGenerator(
                    lookupText,
                    parsedDirective.DirectiveText,
                    parsedDirective.TypePattern,
                    parsedDirective.AssemblyName,
                    errors);
            });

        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
    }

    [Conditional("DEBUG")]
    protected void AssertDirective(string directive)
    {
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Identifier || CurrentToken.Kind == SyntaxKind.Keyword);
        Debug.Assert(string.Equals(CurrentToken.Content, directive, StringComparison.Ordinal));
    }

    private RazorDirectiveBodySyntax ParseTagHelperDirective(
        string keyword,
        Func<string, List<RazorDiagnostic>, SourceLocation, ISpanChunkGenerator> chunkGeneratorFactory)
    {
        AssertDirective(keyword);

        var savedErrorSink = Context.ErrorSink;
        var directiveErrorSink = new ErrorSink();
        RazorMetaCodeSyntax keywordBlock = null;
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var directiveBuilder = pooledResult.Builder;
            Context.ErrorSink = directiveErrorSink;

            string directiveValue = null;
            SourceLocation? valueStartLocation = null;
            try
            {
                EnsureDirectiveIsAtStartOfLine();

                var keywordStartLocation = CurrentStart;

                // Accept the directive name
                var keywordToken = EatCurrentToken();
                var keywordLength = keywordToken.FullWidth + 1 /* @ */;

                var foundWhitespace = At(SyntaxKind.Whitespace);

                // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
                // to the document.  We can't accept it.
                var acceptedCharacters = foundWhitespace ? AcceptedCharactersInternal.None : AcceptedCharactersInternal.AnyExceptNewline;
                Accept(keywordToken);
                keywordBlock = OutputAsMetaCode(Output(), acceptedCharacters);

                AcceptWhile(SyntaxKind.Whitespace);
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                SpanContext.EditHandler.AcceptedCharacters = acceptedCharacters;
                directiveBuilder.Add(OutputAsMarkupLiteral());

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
                    valueStartLocation = CurrentStart;

                    // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                    // etc.
                    AcceptUntil(SyntaxKind.NewLine);

                    // Pull out the value and remove whitespaces and optional quotes
                    var rawValue = string.Concat(TokenBuilder.ToList().Nodes.Select(s => s.Content)).Trim();

                    var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                    var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                    if (startsWithQuote != endsWithQuote)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_IncompleteQuotesAroundDirective(
                                new SourceSpan(valueStartLocation.Value, rawValue.Length), keyword));
                    }

                    directiveValue = rawValue;
                }
            }
            finally
            {
                SpanContext.ChunkGenerator = chunkGeneratorFactory(
                    directiveValue,
                    directiveErrorSink.Errors.ToList(),
                    valueStartLocation ?? CurrentStart);
                Context.ErrorSink = savedErrorSink;
            }

            // Finish the block and output the tokens
            CompleteBlock();
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.AnyExceptNewline;

            directiveBuilder.Add(OutputTokensAsStatementLiteral());
            var directiveCodeBlock = SyntaxFactory.CSharpCodeBlock(directiveBuilder.ToList());

            return SyntaxFactory.RazorDirectiveBody(keywordBlock, directiveCodeBlock);
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
        if (TokenBuilder.Count == 1 &&
            TokenBuilder[0] is SyntaxToken token &&
            token.Kind == SyntaxKind.StringLiteral)
        {
            offset += token.Content.IndexOf(directiveText, StringComparison.Ordinal);

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
            text.StartsWith("'", StringComparison.Ordinal) ||
            text.EndsWith("'", StringComparison.Ordinal))
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

    private void ParseExtensibleDirective(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition, DirectiveDescriptor descriptor)
    {
        AssertDirective(descriptor.Directive);

        var directiveErrorSink = new ErrorSink();
        var savedErrorSink = Context.ErrorSink;
        Context.ErrorSink = directiveErrorSink;

        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var directiveBuilder = pooledResult.Builder;
            RazorMetaCodeSyntax keywordBlock = null;

            try
            {
                EnsureDirectiveIsAtStartOfLine();
                var directiveStart = CurrentStart;
                if (transition != null)
                {
                    // Start the error from the Transition '@'.
                    directiveStart = new SourceLocation(
                        directiveStart.FilePath,
                        directiveStart.AbsoluteIndex - 1,
                        directiveStart.LineIndex,
                        directiveStart.CharacterIndex - 1);
                }

                AcceptAndMoveNext();
                keywordBlock = OutputAsMetaCode(Output());

                // Even if an error was logged do not bail out early. If a directive was used incorrectly it doesn't mean it can't be parsed.
                ValidateDirectiveUsage(descriptor, directiveStart);

                // Capture the last member for validating generic type constraints.
                // Generic type parameters are described by a member token followed by a generic constraint token.
                // The generic constraint token includes the 'where' keyword, the identifier it applies and the constraint list and is represented as a token list.
                // For the directive to be valid we need to check that the identifier for the member token matches the identifier in the generic constraint token.
                // Once we are parsing the constraint token we have lost "easy" access to the identifier for the member. To avoid having complex logic in the generic
                // constraint token parsing code, we instead keep track of the last identifier we've seen on a member token and use that information to check the
                // identifier for the constraint an emit a diagnostic in case they are not the same.
                string lastSeenMemberIdentifier = null;

                for (var i = 0; i < descriptor.Tokens.Count; i++)
                {
                    if (!At(SyntaxKind.Whitespace) &&
                        !At(SyntaxKind.NewLine) &&
                        !At(SyntaxKind.Semicolon) &&
                        !EndOfFile)
                    {
                        // This case should never happen in a real scenario. We're just being defensive.
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_DirectiveTokensMustBeSeparatedByWhitespace(
                                new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));

                        builder.Add(BuildDirective());
                        return;
                    }

                    var tokenDescriptor = descriptor.Tokens[i];

                    if (At(SyntaxKind.Whitespace))
                    {
                        AcceptWhile(IsSpacingTokenIncludingComments);

                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;

                        if (tokenDescriptor.Kind == DirectiveTokenKind.Member ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Namespace ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Type ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Attribute ||
                            tokenDescriptor.Kind == DirectiveTokenKind.GenericTypeConstraint ||
                            tokenDescriptor.Kind == DirectiveTokenKind.Boolean)
                        {
                            directiveBuilder.Add(OutputTokensAsStatementLiteral());

                            if (EndOfFile || At(SyntaxKind.NewLine))
                            {
                                // Add a marker token to provide CSharp intellisense when we start typing the directive token.
                                // We want CSharp intellisense only if there is whitespace after the directive keyword.
                                AcceptMarkerTokenIfNecessary();
                                SpanContext.ChunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                                SpanContext.EditHandler = new DirectiveTokenEditHandler(LanguageTokenizeString);
                                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.NonWhitespace;
                                directiveBuilder.Add(OutputTokensAsStatementLiteral());
                            }
                        }
                        else
                        {
                            directiveBuilder.Add(OutputAsMarkupEphemeralLiteral());
                        }
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
                        builder.Add(BuildDirective());
                        return;
                    }

                    switch (tokenDescriptor.Kind)
                    {
                        case DirectiveTokenKind.Type:
                            if (!TryParseNamespaceOrTypeName(directiveBuilder))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsTypeName(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));

                                builder.Add(BuildDirective());
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Namespace:
                            if (!TryParseQualifiedIdentifier(out var identifierLength))
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsNamespace(
                                        new SourceSpan(CurrentStart, identifierLength), descriptor.Directive));

                                builder.Add(BuildDirective());
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Member:
                            if (At(SyntaxKind.Identifier))
                            {
                                lastSeenMemberIdentifier = CurrentToken.Content;
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective());
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
                                builder.Add(BuildDirective());
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Boolean:
                            if (AtBooleanLiteral() && !CurrentToken.ContainsDiagnostics)
                            {
                                AcceptAndMoveNext();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsBooleanLiteral(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective());
                                return;
                            }
                            break;

                        case DirectiveTokenKind.Attribute:
                            if (At(SyntaxKind.LeftBracket))
                            {
                                if (Balance(directiveBuilder, BalancingModes.NoErrorOnFailure))
                                {
                                    TryAccept(SyntaxKind.RightBracket);
                                }
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_DirectiveExpectsCSharpAttribute(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive));
                                builder.Add(BuildDirective());
                                return;
                            }

                            break;
                        case DirectiveTokenKind.GenericTypeConstraint:
                            if (At(SyntaxKind.Keyword) &&
                                string.Equals(CurrentToken.Content, CSharpLanguageCharacteristics.GetKeyword(CSharpKeyword.Where), StringComparison.Ordinal))
                            {
                                // Consume the 'where' keyword plus any aditional whitespace
                                AcceptAndMoveNext();
                                AcceptWhile(SyntaxKind.Whitespace);
                                // Check that the type name matches the type name before the where clause.
                                // Find a better way to do this
                                if (!string.Equals(CurrentToken.Content, lastSeenMemberIdentifier, StringComparison.Ordinal))
                                {
                                    // @typeparam TKey where TValue : ...
                                    // The type parameter in the generic type constraint 'TValue' does not match the type parameter 'TKey' defined in the directive '@typeparam'.
                                    Context.ErrorSink.OnError(
                                        RazorDiagnosticFactory.CreateParsing_GenericTypeParameterIdentifierMismatch(
                                            new SourceSpan(CurrentStart, CurrentToken.Content.Length), descriptor.Directive, CurrentToken.Content, lastSeenMemberIdentifier));
                                    builder.Add(BuildDirective());
                                    return;
                                }
                                else
                                {
                                    while (!At(SyntaxKind.NewLine))
                                    {
                                        if (At(SyntaxKind.Semicolon))
                                        {
                                            // Consume the ending ';'
                                            EatCurrentToken();
                                            break;
                                        }

                                        AcceptAndMoveNext();
                                        if (EndOfFile)
                                        {
                                            // We've reached the end of the file, which is unusual but can happen, for example if we start typing in a new file.
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (At(SyntaxKind.Semicolon))
                            {
                                // Consume the ending ';'
                                EatCurrentToken();
                            }
                            else
                            {
                                Context.ErrorSink.OnError(
                                    RazorDiagnosticFactory.CreateParsing_UnexpectedIdentifier(
                                        new SourceSpan(CurrentStart, CurrentToken.Content.Length),
                                        CurrentToken.Content,
                                        CSharpLanguageCharacteristics.GetKeyword(CSharpKeyword.Where)));

                                builder.Add(BuildDirective());
                                return;
                            }

                            break;
                    }

                    SpanContext.ChunkGenerator = new DirectiveTokenChunkGenerator(tokenDescriptor);
                    SpanContext.EditHandler = new DirectiveTokenEditHandler(LanguageTokenizeString);
                    SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.NonWhitespace;
                    directiveBuilder.Add(OutputTokensAsStatementLiteral());
                }

                AcceptWhile(IsSpacingTokenIncludingComments);
                SpanContext.ChunkGenerator = SpanChunkGenerator.Null;

                switch (descriptor.Kind)
                {
                    case DirectiveKind.SingleLine:
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        TryAccept(SyntaxKind.Semicolon);
                        directiveBuilder.Add(OutputAsMetaCode(Output(), AcceptedCharactersInternal.Whitespace));

                        AcceptWhile(IsSpacingTokenIncludingComments);

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


                        // This should contain the optional whitespace after the optional semicolon and the new line.
                        // Output as Markup as we want intellisense here.
                        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Whitespace;
                        directiveBuilder.Add(OutputAsMarkupEphemeralLiteral());
                        break;
                    case DirectiveKind.RazorBlock:
                        AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.AllWhitespace;
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        ParseDirectiveBlock(directiveBuilder, descriptor, parseChildren: (childBuilder, startingBraceLocation) =>
                        {
                                // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
                                // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
                                // as C#; it should be handled as a period because it's wrapped in markup.
                                var wasNested = IsNested;
                            IsNested = false;

                            using (PushSpanContextConfig())
                            {
                                var razorBlock = HtmlParser.ParseRazorBlock(Tuple.Create("{", "}"), caseSensitive: true);
                                directiveBuilder.Add(razorBlock);
                            }

                            InitializeContext(SpanContext);
                            IsNested = wasNested;
                            NextToken();
                        });
                        break;
                    case DirectiveKind.CodeBlock:
                        AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);
                        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.AllWhitespace;
                        directiveBuilder.Add(OutputTokensAsUnclassifiedLiteral());

                        ParseDirectiveBlock(directiveBuilder, descriptor, parseChildren: (childBuilder, startingBraceLocation) =>
                        {
                            NextToken();

                            var existingEditHandler = SpanContext.EditHandler;
                            SpanContext.EditHandler = new CodeBlockEditHandler(LanguageTokenizeString);

                            if (Context.FeatureFlags.AllowRazorInAllCodeBlocks)
                            {
                                var block = new Block(descriptor.Directive, directiveStart);
                                ParseCodeBlock(childBuilder, block);
                            }
                            else
                            {
                                Balance(childBuilder, BalancingModes.NoErrorOnFailure, SyntaxKind.LeftBrace, SyntaxKind.RightBrace, startingBraceLocation);
                            }

                            SpanContext.ChunkGenerator = new StatementChunkGenerator();

                            AcceptMarkerTokenIfNecessary();

                            childBuilder.Add(OutputTokensAsStatementLiteral());

                            SpanContext.EditHandler = existingEditHandler;
                        });
                        break;
                }
            }
            finally
            {
                Context.ErrorSink = savedErrorSink;
            }

            builder.Add(BuildDirective());

            RazorDirectiveSyntax BuildDirective()
            {
                directiveBuilder.Add(OutputTokensAsStatementLiteral());
                var directiveCodeBlock = SyntaxFactory.CSharpCodeBlock(directiveBuilder.ToList());

                var directiveBody = SyntaxFactory.RazorDirectiveBody(keywordBlock, directiveCodeBlock);
                var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
                directive = (RazorDirectiveSyntax)directive.SetDiagnostics(directiveErrorSink.Errors.ToArray());
                directive = directive.WithDirectiveDescriptor(descriptor);
                return directive;
            }
        }
    }

    private void ValidateDirectiveUsage(DirectiveDescriptor descriptor, SourceLocation directiveStart)
    {
        if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
        {
            if (Context.SeenDirectives.Contains(descriptor.Directive))
            {
                // There will always be at least 1 child because of the `@` transition.
                var errorLength = /* @ */ 1 + descriptor.Directive.Length;
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                        new SourceSpan(directiveStart, errorLength), descriptor.Directive));

                return;
            }
        }
    }

    // Used for parsing a qualified name like that which follows the `namespace` keyword.
    //
    // qualified-identifier:
    //      identifier
    //      qualified-identifier . identifier
    protected bool TryParseQualifiedIdentifier(out int identifierLength)
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

    private void ParseDirectiveBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, DirectiveDescriptor descriptor, Action<SyntaxListBuilder<RazorSyntaxNode>, SourceLocation> parseChildren)
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
            var editHandler = new AutoCompleteEditHandler(LanguageTokenizeString, autoCompleteAtEndOfSpan: true);
            SpanContext.EditHandler = editHandler;
            var startingBraceLocation = CurrentStart;
            Accept(CurrentToken);
            builder.Add(OutputAsMetaCode(Output()));

            using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
            {
                var childBuilder = pooledResult.Builder;
                parseChildren(childBuilder, startingBraceLocation);
                if (childBuilder.Count > 0)
                {
                    builder.Add(SyntaxFactory.CSharpCodeBlock(childBuilder.ToList()));
                }
            }

            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            if (!TryAccept(SyntaxKind.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(startingBraceLocation, contentLength: 1 /* } */), descriptor.Directive, "}", "{"));

                Accept(SyntaxFactory.MissingToken(SyntaxKind.RightBrace));
            }
            else
            {
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }
            CompleteBlock(insertMarkerIfNecessary: false, captureWhitespaceToEndOfLine: true);
            builder.Add(OutputAsMetaCode(Output(), SpanContext.EditHandler.AcceptedCharacters));
        }
    }

    private bool TryParseKeyword(in SyntaxListBuilder<RazorSyntaxNode> builder, IReadOnlyList<SyntaxToken> whitespace, CSharpTransitionSyntax transition)
    {
        var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(CurrentToken.Kind == SyntaxKind.Keyword && result.HasValue);
        if (_keywordParserMap.TryGetValue(result.Value, out var handler))
        {
            if (whitespace != null)
            {
                // This is a keyword. We want to preserve preceding whitespace in the output.
                Accept(whitespace);
                builder.Add(OutputTokensAsStatementLiteral());
            }

            handler(builder, transition);
            return true;
        }

        return false;
    }

    private bool AtBooleanLiteral()
    {
        var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
        return result.HasValue && (result.Value == CSharpKeyword.True || result.Value == CSharpKeyword.False);
    }

    private void SetupExpressionParsers()
    {
        MapExpressionKeyword(ParseAwaitExpression, CSharpKeyword.Await);
    }

    private void SetupKeywordParsers()
    {
        MapKeywords(
            ParseConditionalBlock,
            CSharpKeyword.For,
            CSharpKeyword.Foreach,
            CSharpKeyword.While,
            CSharpKeyword.Switch,
            CSharpKeyword.Lock);
        MapKeywords(ParseCaseStatement, false, CSharpKeyword.Case, CSharpKeyword.Default);
        MapKeywords(ParseIfStatement, CSharpKeyword.If);
        MapKeywords(ParseTryStatement, CSharpKeyword.Try);
        MapKeywords(ParseDoStatement, CSharpKeyword.Do);
        MapKeywords(ParseUsingKeyword, CSharpKeyword.Using);
    }

    private void MapExpressionKeyword(Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax> handler, CSharpKeyword keyword)
    {
        _keywordParserMap.Add(keyword, handler);

        // Expression keywords don't belong in the regular keyword list
    }

    private void MapKeywords(Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax> handler, params CSharpKeyword[] keywords)
    {
        MapKeywords(handler, topLevel: true, keywords: keywords);
    }

    private void MapKeywords(Action<SyntaxListBuilder<RazorSyntaxNode>, CSharpTransitionSyntax> handler, bool topLevel, params CSharpKeyword[] keywords)
    {
        foreach (var keyword in keywords)
        {
            _keywordParserMap.Add(keyword, handler);
            if (topLevel)
            {
                Keywords.Add(CSharpLanguageCharacteristics.GetKeyword(keyword));
            }
        }
    }

    private void ParseAwaitExpression(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        // Ensure that we're on the await statement (only runs in debug)
        Assert(CSharpKeyword.Await);

        // Accept the "await" and move on
        AcceptAndMoveNext();

        // Accept 1 or more spaces between the await and the following code.
        AcceptWhile(IsSpacingTokenIncludingComments);

        // Top level basically indicates if we're within an expression or statement.
        // Ex: topLevel true = @await Foo()  |  topLevel false = @{ await Foo(); }
        // Note that in this case @{ <b>@await Foo()</b> } top level is true for await.
        // Therefore, if we're top level then we want to act like an implicit expression,
        // otherwise just act as whatever we're contained in.
        var topLevel = transition != null;
        if (!topLevel)
        {
            return;
        }

        if (At(CSharpKeyword.Foreach))
        {
            // C# 8 async streams. @await foreach (var value in asyncEnumerable) { .... }

            ParseConditionalBlock(builder, transition);
        }
        else
        {
            // Setup the Span to be an async implicit expression (an implicit expresison that allows spaces).
            // Spaces are allowed because of "@await Foo()".
            var implicitExpressionBody = ParseImplicitExpressionBody(async: true);
            builder.Add(SyntaxFactory.CSharpImplicitExpression(transition, implicitExpressionBody));
        }
    }

    private void ParseConditionalBlock(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        var topLevel = transition != null;
        ParseConditionalBlock(builder, transition, topLevel);
    }

    private void ParseConditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition, bool topLevel)
    {
        Assert(SyntaxKind.Keyword);
        if (transition != null)
        {
            builder.Add(transition);
        }

        var block = new Block(CurrentToken, CurrentStart);
        ParseConditionalBlock(builder, block);
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseConditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);

        // Parse the condition, if present (if not present, we'll let the C# compiler complain)
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);

            ParseExpectedCodeBlock(builder, block);
        }
    }

    private bool TryParseCondition(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (At(SyntaxKind.LeftParenthesis))
        {
            var complete = Balance(builder, BalancingModes.BacktrackOnFailure | BalancingModes.AllowCommentsAndTemplates);
            if (!complete)
            {
                AcceptUntil(SyntaxKind.NewLine);
            }
            else
            {
                TryAccept(SyntaxKind.RightParenthesis);
            }
            return complete;
        }
        return true;
    }

    private void ParseExpectedCodeBlock(in SyntaxListBuilder<RazorSyntaxNode> builder, Block block)
    {
        if (!EndOfFile)
        {
            // If it's a block control flow statement the current syntax token will be a LeftBrace {,
            // otherwise we're acting on a single line control flow statement which cannot allow markup.

            if (At(SyntaxKind.LessThan))
            {
                // if (...) <p>Hello World</p>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, CurrentToken.Content.Length)));
            }
            else if (At(SyntaxKind.Transition) && NextIs(SyntaxKind.Colon))
            {
                // if (...) @: <p>The time is @DateTime.Now</p>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, contentLength: 2 /* @: */)));
            }
            else if (At(SyntaxKind.Transition) && NextIs(SyntaxKind.Transition))
            {
                // if (...) @@JohnDoe <strong>Hi!</strong>
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(
                        new SourceSpan(CurrentStart, contentLength: 2 /* @@ */)));
            }

            // Parse the statement and then we're done
            ParseStatement(builder, block);
        }
    }

    private void ParseUnconditionalBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(SyntaxKind.Keyword);
        var block = new Block(CurrentToken, CurrentStart);
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);
        ParseExpectedCodeBlock(builder, block);
    }

    private void ParseCaseStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Assert(SyntaxKind.Keyword);
        if (transition != null)
        {
            // Normally, case statement won't start with a transition in a valid scenario.
            // If it does, just accept it and let the compiler complain.
            builder.Add(transition);
        }
        var result = CSharpTokenizer.GetTokenKeyword(CurrentToken);
        Debug.Assert(result.HasValue &&
                     (result.Value == CSharpKeyword.Case ||
                      result.Value == CSharpKeyword.Default));
        AcceptUntil(SyntaxKind.Colon);
        TryAccept(SyntaxKind.Colon);
    }

    private void ParseIfStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Assert(CSharpKeyword.If);
        ParseConditionalBlock(builder, transition, topLevel: false);
        ParseAfterIfClause(builder);
        var topLevel = transition != null;
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseAfterIfClause(SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Grab whitespace and razor comments
        var whitespace = SkipToNextImportantToken(builder);

        // Check for an else part
        if (At(CSharpKeyword.Else))
        {
            Accept(whitespace);
            Assert(CSharpKeyword.Else);
            ParseElseClause(builder);
        }
        else
        {
            // No else, return whitespace
            PutCurrentBack();
            PutBack(whitespace);
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
        }
    }

    private void ParseElseClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (!At(CSharpKeyword.Else))
        {
            return;
        }
        var block = new Block(CurrentToken, CurrentStart);

        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);
        if (At(CSharpKeyword.If))
        {
            // ElseIf
            block.Name = SyntaxConstants.CSharp.ElseIfKeyword;
            ParseConditionalBlock(builder, block);
            ParseAfterIfClause(builder);
        }
        else if (!EndOfFile)
        {
            // Else
            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseTryStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Assert(CSharpKeyword.Try);
        var topLevel = transition != null;
        if (topLevel)
        {
            builder.Add(transition);
        }

        ParseUnconditionalBlock(builder);
        ParseAfterTryClause(builder);
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseAfterTryClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // Grab whitespace
        var whitespace = SkipToNextImportantToken(builder);

        // Check for a catch or finally part
        if (At(CSharpKeyword.Catch))
        {
            Accept(whitespace);
            Assert(CSharpKeyword.Catch);
            ParseFilterableCatchBlock(builder);
            ParseAfterTryClause(builder);
        }
        else if (At(CSharpKeyword.Finally))
        {
            Accept(whitespace);
            Assert(CSharpKeyword.Finally);
            ParseUnconditionalBlock(builder);
        }
        else
        {
            // Return whitespace and end the block
            PutCurrentBack();
            PutBack(whitespace);
            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
        }
    }

    private void ParseFilterableCatchBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(CSharpKeyword.Catch);

        var block = new Block(CurrentToken, CurrentStart);

        // Accept "catch"
        AcceptAndMoveNext();
        AcceptWhile(IsValidStatementSpacingToken);

        // Parse the catch condition if present. If not present, let the C# compiler complain.
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsValidStatementSpacingToken);

            if (At(CSharpKeyword.When))
            {
                // Accept "when".
                AcceptAndMoveNext();
                AcceptWhile(IsValidStatementSpacingToken);

                // Parse the filter condition if present. If not present, let the C# compiler complain.
                if (!TryParseCondition(builder))
                {
                    // Incomplete condition.
                    return;
                }

                AcceptWhile(IsValidStatementSpacingToken);
            }

            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseDoStatement(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Assert(CSharpKeyword.Do);
        if (transition != null)
        {
            builder.Add(transition);
        }

        ParseUnconditionalBlock(builder);
        ParseWhileClause(builder);
        var topLevel = transition != null;
        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseWhileClause(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
        var whitespace = SkipToNextImportantToken(builder);

        if (At(CSharpKeyword.While))
        {
            Accept(whitespace);
            Assert(CSharpKeyword.While);
            AcceptAndMoveNext();
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);
            if (TryParseCondition(builder) && TryAccept(SyntaxKind.Semicolon))
            {
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            }
        }
        else
        {
            PutCurrentBack();
            PutBack(whitespace);
        }
    }

    private void ParseUsingKeyword(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Assert(CSharpKeyword.Using);
        var topLevel = transition != null;
        var block = new Block(CurrentToken, CurrentStart);
        var usingToken = EatCurrentToken();
        var whitespaceOrComments = ReadWhile(IsSpacingTokenIncludingComments);
        var atLeftParen = At(SyntaxKind.LeftParenthesis);
        var atIdentifier = At(SyntaxKind.Identifier);
        var atStatic = At(CSharpKeyword.Static);

        // Put the read tokens back and let them be handled later.
        PutCurrentBack();
        PutBack(whitespaceOrComments);
        PutBack(usingToken);
        EnsureCurrent();

        if (atLeftParen)
        {
            // using ( ==> Using Statement
            ParseUsingStatement(builder, transition, block);
        }
        else if (atIdentifier || atStatic)
        {
            // using Identifier ==> Using Declaration
            if (!topLevel)
            {
                // using Variable Declaration

                if (!Context.FeatureFlags.AllowUsingVariableDeclarations)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock(
                            new SourceSpan(block.Start, block.Name.Length)));
                }

                // There are cases when a user will do @using var x = 123; At which point we let C# notify the user
                // of their error like we do any other invalid expression.
                if (transition != null)
                {
                    builder.Add(transition);
                }
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingTokenIncludingComments);
                ParseStandardStatement(builder);
            }
            else
            {
                ParseUsingDeclaration(builder, transition);
                return;
            }
        }
        else
        {
            if (transition != null)
            {
                builder.Add(transition);
            }

            AcceptAndMoveNext();
            AcceptWhile(IsSpacingTokenIncludingComments);
        }

        if (topLevel)
        {
            CompleteBlock();
        }
    }

    private void ParseUsingStatement(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition, Block block)
    {
        Assert(CSharpKeyword.Using);
        AcceptAndMoveNext();
        AcceptWhile(IsSpacingTokenIncludingComments);

        Assert(SyntaxKind.LeftParenthesis);
        if (transition != null)
        {
            builder.Add(transition);
        }

        // Parse condition
        if (TryParseCondition(builder))
        {
            AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);

            // Parse code block
            ParseExpectedCodeBlock(builder, block);
        }
    }

    private void ParseUsingDeclaration(in SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        // Using declarations should always be top level. The error case is handled in a different code path.
        Debug.Assert(transition != null);
        using (var pooledResult = Pool.Allocate<RazorSyntaxNode>())
        {
            var directiveBuilder = pooledResult.Builder;
            Assert(CSharpKeyword.Using);
            AcceptAndMoveNext();
            var isStatic = false;
            var nonNamespaceTokenCount = TokenBuilder.Count;
            AcceptWhile(IsSpacingTokenIncludingComments);
            var start = CurrentStart;
            if (At(SyntaxKind.Identifier))
            {
                // non-static using
                nonNamespaceTokenCount = TokenBuilder.Count;
                TryParseNamespaceOrTypeName(directiveBuilder);
                var whitespace = ReadWhile(IsSpacingTokenIncludingNewLinesAndComments);
                if (At(SyntaxKind.Assign))
                {
                    // Alias
                    Accept(whitespace);
                    Assert(SyntaxKind.Assign);
                    AcceptAndMoveNext();

                    AcceptWhile(IsSpacingTokenIncludingNewLinesAndComments);

                    // One more namespace or type name
                    TryParseNamespaceOrTypeName(directiveBuilder);
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
                isStatic = true;
                AcceptAndMoveNext();
                AcceptWhile(IsSpacingTokenIncludingComments);
                nonNamespaceTokenCount = TokenBuilder.Count;
                TryParseNamespaceOrTypeName(directiveBuilder);
            }

            var usingStatementTokens = TokenBuilder.ToList().Nodes;
            var usingContentTokens = usingStatementTokens.Skip(1);
            var parsedNamespaceTokens = usingStatementTokens
                .Skip(nonNamespaceTokenCount)
                .Where(s => s.Kind != SyntaxKind.CSharpComment && s.Kind != SyntaxKind.Whitespace && s.Kind != SyntaxKind.NewLine);

            SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.AnyExceptNewline;
            SpanContext.ChunkGenerator = new AddImportChunkGenerator(
                string.Concat(usingContentTokens.Select(s => s.Content)),
                string.Concat(parsedNamespaceTokens.Select(s => s.Content)),
                isStatic);

            // Optional ";"
            if (EnsureCurrent())
            {
                TryAccept(SyntaxKind.Semicolon);
            }

            CompleteBlock();
            Debug.Assert(directiveBuilder.Count == 0, "We should not have built any blocks so far.");
            var keywordTokens = OutputTokensAsStatementLiteral();
            var directiveBody = SyntaxFactory.RazorDirectiveBody(keywordTokens, null);
            builder.Add(SyntaxFactory.RazorDirective(transition, directiveBody));
        }
    }

    private bool TryParseNamespaceOrTypeName(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (TryAccept(SyntaxKind.LeftParenthesis))
        {
            while (!TryAccept(SyntaxKind.RightParenthesis) && !EndOfFile)
            {
                TryAccept(SyntaxKind.Whitespace);

                if (!TryParseNamespaceOrTypeName(builder))
                {
                    return false;
                }

                TryAccept(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.Identifier);
                TryAccept(SyntaxKind.Whitespace);
                TryAccept(SyntaxKind.Comma);
            }

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            TryAccept(SyntaxKind.QuestionMark); // Nullable

            return true;
        }
        else if (TryAccept(SyntaxKind.Identifier) || TryAccept(SyntaxKind.Keyword))
        {
            if (TryAccept(SyntaxKind.DoubleColon))
            {
                if (!TryAccept(SyntaxKind.Identifier))
                {
                    TryAccept(SyntaxKind.Keyword);
                }
            }
            if (At(SyntaxKind.LessThan))
            {
                ParseTypeArgumentList(builder);
            }
            if (TryAccept(SyntaxKind.Dot))
            {
                TryParseNamespaceOrTypeName(builder);
            }

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.QuestionMark))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            TryAccept(SyntaxKind.QuestionMark); // Nullable

            if (At(SyntaxKind.Whitespace) && NextIs(SyntaxKind.LeftBracket))
            {
                // Only accept the whitespace if we are going to consume the next token.
                AcceptAndMoveNext();
            }

            while (At(SyntaxKind.LeftBracket))
            {
                Balance(builder, BalancingModes.None);
                if (!TryAccept(SyntaxKind.RightBracket))
                {
                    Accept(SyntaxFactory.MissingToken(SyntaxKind.RightBracket));
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ParseTypeArgumentList(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        Assert(SyntaxKind.LessThan);
        Balance(builder, BalancingModes.None);
        if (!TryAccept(SyntaxKind.GreaterThan))
        {
            Accept(SyntaxFactory.MissingToken(SyntaxKind.GreaterThan));
        }
    }

    private void ParseReservedDirective(SyntaxListBuilder<RazorSyntaxNode> builder, CSharpTransitionSyntax transition)
    {
        Context.ErrorSink.OnError(
            RazorDiagnosticFactory.CreateParsing_ReservedWord(
                new SourceSpan(CurrentStart, CurrentToken.Content.Length), CurrentToken.Content));

        AcceptAndMoveNext();
        SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
        SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
        CompleteBlock();
        var keyword = OutputAsMetaCode(Output());
        var directiveBody = SyntaxFactory.RazorDirectiveBody(keyword, cSharpCode: null);

        // transition could be null if we're already inside a code block.
        transition = transition ?? SyntaxFactory.CSharpTransition(SyntaxFactory.MissingToken(SyntaxKind.Transition));
        var directive = SyntaxFactory.RazorDirective(transition, directiveBody);
        builder.Add(directive);
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
        if (insertMarkerIfNecessary && Context.LastAcceptedCharacters != AcceptedCharactersInternal.Any)
        {
            AcceptMarkerTokenIfNecessary();
        }

        EnsureCurrent();

        // Read whitespace, but not newlines
        // If we're not inserting a marker span, we don't need to capture whitespace
        if (!Context.WhiteSpaceIsSignificantToAncestorBlock &&
            captureWhitespaceToEndOfLine &&
            !Context.DesignTimeMode &&
            !IsNested)
        {
            var whitespace = ReadWhile(static token => token.Kind == SyntaxKind.Whitespace);
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
        else
        {
            PutCurrentBack();
        }
    }

    private IReadOnlyList<SyntaxToken> SkipToNextImportantToken(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        while (!EndOfFile)
        {
            var whitespace = ReadWhile(IsSpacingTokenIncludingNewLinesAndComments);
            if (At(SyntaxKind.RazorCommentTransition))
            {
                Accept(whitespace);
                SpanContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
                AcceptMarkerTokenIfNecessary();
                builder.Add(OutputTokensAsStatementLiteral());
                var comment = ParseRazorComment();
                builder.Add(comment);
            }
            else
            {
                return whitespace;
            }
        }
        return Array.Empty<SyntaxToken>();
    }

    private void DefaultSpanContextConfig(SpanContextBuilder spanContext)
    {
        spanContext.EditHandler = SpanEditHandler.CreateDefault(LanguageTokenizeString);
        spanContext.ChunkGenerator = new StatementChunkGenerator();
    }

    private void ExplicitExpressionSpanContextConfig(SpanContextBuilder spanContext)
    {
        spanContext.EditHandler = SpanEditHandler.CreateDefault(LanguageTokenizeString);
        spanContext.ChunkGenerator = new ExpressionChunkGenerator();
    }

    private CSharpStatementLiteralSyntax OutputTokensAsStatementLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return GetNodeWithSpanContext(SyntaxFactory.CSharpStatementLiteral(tokens));
    }

    private CSharpExpressionLiteralSyntax OutputTokensAsExpressionLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return GetNodeWithSpanContext(SyntaxFactory.CSharpExpressionLiteral(tokens));
    }

    private CSharpEphemeralTextLiteralSyntax OutputTokensAsEphemeralLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }

        return GetNodeWithSpanContext(SyntaxFactory.CSharpEphemeralTextLiteral(tokens));
    }

    private UnclassifiedTextLiteralSyntax OutputTokensAsUnclassifiedLiteral()
    {
        var tokens = Output();
        if (tokens.Count == 0)
        {
            return null;
        }


        return GetNodeWithSpanContext(SyntaxFactory.UnclassifiedTextLiteral(tokens));
    }

    private void OtherParserBlock(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        // When transitioning to the HTML parser we no longer want to act as if we're in a nested C# state.
        // For instance, if <div>@hello.</div> is in a nested C# block we don't want the trailing '.' to be handled
        // as C#; it should be handled as a period because it's wrapped in markup.
        var wasNested = IsNested;
        IsNested = false;

        RazorSyntaxNode htmlBlock = null;
        using (PushSpanContextConfig())
        {
            htmlBlock = HtmlParser.ParseBlock();
        }

        builder.Add(htmlBlock);
        InitializeContext(SpanContext);

        IsNested = wasNested;
        NextToken();
    }

    private bool Balance(SyntaxListBuilder<RazorSyntaxNode> builder, BalancingModes mode)
    {
        var left = CurrentToken.Kind;
        var right = Language.FlipBracket(left);
        var start = CurrentStart;
        AcceptAndMoveNext();
        if (EndOfFile && ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure))
        {
            Context.ErrorSink.OnError(
                RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                    new SourceSpan(start, contentLength: 1 /* { OR } */),
                    Language.GetSample(left),
                    Language.GetSample(right)));
        }

        return Balance(builder, mode, left, right, start);
    }

    private bool Balance(SyntaxListBuilder<RazorSyntaxNode> builder, BalancingModes mode, SyntaxKind left, SyntaxKind right, SourceLocation start)
    {
        var startPosition = CurrentStart.AbsoluteIndex;
        var nesting = 1;
        var stopAtEndOfLine = (mode & BalancingModes.StopAtEndOfLine) == BalancingModes.StopAtEndOfLine;
        if (!EndOfFile &&
            !(stopAtEndOfLine && At(SyntaxKind.NewLine)))
        {
            var tokens = new List<SyntaxToken>();
            do
            {
                if (IsAtEmbeddedTransition(
                    (mode & BalancingModes.AllowCommentsAndTemplates) == BalancingModes.AllowCommentsAndTemplates,
                    (mode & BalancingModes.AllowEmbeddedTransitions) == BalancingModes.AllowEmbeddedTransitions))
                {
                    Accept(tokens);
                    tokens.Clear();
                    ParseEmbeddedTransition(builder);

                    // Reset backtracking since we've already outputted some spans.
                    startPosition = CurrentStart.AbsoluteIndex;
                }
                if (At(left))
                {
                    nesting++;
                }
                else if (At(right))
                {
                    nesting--;
                }
                if (nesting > 0)
                {
                    tokens.Add(CurrentToken);
                }
            }
            while (nesting > 0 && NextToken() && !(stopAtEndOfLine && At(SyntaxKind.NewLine)));

            if (nesting > 0)
            {
                if ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure)
                {
                    Context.ErrorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                            new SourceSpan(start, contentLength: 1 /* { OR } */),
                            Language.GetSample(left),
                            Language.GetSample(right)));
                }
                if ((mode & BalancingModes.BacktrackOnFailure) == BalancingModes.BacktrackOnFailure)
                {
                    Context.Source.Position = startPosition;
                    NextToken();
                }
                else
                {
                    Accept(tokens);
                }
            }
            else
            {
                // Accept all the tokens we saw
                Accept(tokens);
            }
        }
        return nesting == 0;
    }

    private bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
    {
        // No embedded transitions in C#, so ignore that param
        return allowTemplatesAndComments
               && ((Language.IsTransition(CurrentToken)
                    && NextIs(SyntaxKind.LessThan, SyntaxKind.Colon, SyntaxKind.DoubleColon))
                   || Language.IsCommentStart(CurrentToken));
    }

    private void ParseEmbeddedTransition(in SyntaxListBuilder<RazorSyntaxNode> builder)
    {
        if (Language.IsTransition(CurrentToken))
        {
            PutCurrentBack();
            ParseTemplate(builder);
        }
        else if (Language.IsCommentStart(CurrentToken))
        {
            // Output tokens before parsing the comment.
            AcceptMarkerTokenIfNecessary();
            if (SpanContext.ChunkGenerator is ExpressionChunkGenerator)
            {
                builder.Add(OutputTokensAsExpressionLiteral());
            }
            else
            {
                builder.Add(OutputTokensAsStatementLiteral());
            }

            var comment = ParseRazorComment();
            builder.Add(comment);
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
