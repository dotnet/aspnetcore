// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpImplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        private const string TestExtraKeyword = "model";

        public static TheoryData NullConditionalOperatorData_Bracket
        {
            get
            {
                var noErrors = new RazorDiagnostic[0];
                Func<int, RazorDiagnostic[]> missingEndParenError = (index) =>
                    new RazorDiagnostic[1]
                    {
                        RazorDiagnostic.Create(new RazorError(
                            "An opening \"(\" is missing the corresponding closing \")\".",
                            new SourceLocation(index, 0, index),
                            length: 1))
                    };
                Func<int, RazorDiagnostic[]> missingEndBracketError = (index) =>
                    new RazorDiagnostic[1]
                    {
                        RazorDiagnostic.Create(new RazorError(
                            "An opening \"[\" is missing the corresponding closing \"]\".",
                            new SourceLocation(index, 0, index),
                            length: 1))
                    };

                // implicitExpression, expectedImplicitExpression, acceptedCharacters, expectedErrors
                return new TheoryData<string, string, AcceptedCharactersInternal, RazorDiagnostic[]>
                {
                    { "val??[", "val", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val??[0", "val", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[", "val?[", AcceptedCharactersInternal.Any, missingEndBracketError(5) },
                    { "val?(", "val", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[more", "val?[more", AcceptedCharactersInternal.Any, missingEndBracketError(5) },
                    { "val?[0]", "val?[0]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[<p>", "val?[", AcceptedCharactersInternal.Any, missingEndBracketError(5) },
                    { "val?[more.<p>", "val?[more.", AcceptedCharactersInternal.Any, missingEndBracketError(5) },
                    { "val??[more<p>", "val", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[-1]?", "val?[-1]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[abc]?[def", "val?[abc]?[def", AcceptedCharactersInternal.Any, missingEndBracketError(11) },
                    { "val?[abc]?[2]", "val?[abc]?[2]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[abc]?.more?[def]", "val?[abc]?.more?[def]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[abc]?.more?.abc", "val?[abc]?.more?.abc", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[null ?? true]", "val?[null ?? true]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                    { "val?[abc?.gef?[-1]]", "val?[abc?.gef?[-1]]", AcceptedCharactersInternal.NonWhiteSpace, noErrors },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullConditionalOperatorData_Bracket))]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket(
            string implicitExpresison,
            string expectedImplicitExpression,
            object acceptedCharacters,
            object expectedErrors)
        {
            // Act & Assert
            ImplicitExpressionTest(
                implicitExpresison,
                expectedImplicitExpression,
                (AcceptedCharactersInternal)acceptedCharacters,
                (RazorDiagnostic[])expectedErrors);
        }

        public static TheoryData NullConditionalOperatorData_Dot
        {
            get
            {
                // implicitExpression, expectedImplicitExpression
                return new TheoryData<string, string>
                {
                    { "val?", "val" },
                    { "val??", "val" },
                    { "val??more", "val" },
                    { "val?!", "val" },
                    { "val?.", "val?." },
                    { "val??.", "val" },
                    { "val?.(abc)", "val?." },
                    { "val?.<p>", "val?." },
                    { "val?.more", "val?.more" },
                    { "val?.more<p>", "val?.more" },
                    { "val??.more<p>", "val" },
                    { "val?.more(false)?.<p>", "val?.more(false)?." },
                    { "val?.more(false)?.abc", "val?.more(false)?.abc" },
                    { "val?.more(null ?? true)?.abc", "val?.more(null ?? true)?.abc" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullConditionalOperatorData_Dot))]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot(
            string implicitExpresison,
            string expectedImplicitExpression)
        {
            // Act & Assert
            ImplicitExpressionTest(implicitExpresison, expectedImplicitExpression);
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseBlockTest("if (true) { @foo }",
                           new StatementBlock(
                               Factory.Code("if (true) { ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                               Factory.Code(" }").AsStatement()));
        }

        [Fact]
        public void ParseBlockAcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ImplicitExpressionTest("हळूँजद॔.", "हळूँजद॔");
        }

        [Fact]
        public void ParseBlockOutputsZeroLengthCodeSpanIfInvalidCharacterFollowsTransition()
        {
            ParseBlockTest("@/",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1),
                                "/"));
        }

        [Fact]
        public void ParseBlockOutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseBlockTest("@",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1)));
        }

        [Fact]
        public void ParseBlockSupportsSlashesWithinComplexImplicitExpressions()
        {
            ImplicitExpressionTest("DataGridColumn.Template(\"Years of Service\", e => (int)Math.Round((DateTime.Now - dt).TotalDays / 365))");
        }

        [Fact]
        public void ParseBlockMethodParsesSingleIdentifierAsImplicitExpression()
        {
            ImplicitExpressionTest("foo");
        }

        [Fact]
        public void ParseBlockMethodDoesNotAcceptSemicolonIfExpressionTerminatedByWhitespace()
        {
            ImplicitExpressionTest("foo ;", "foo");
        }

        [Fact]
        public void ParseBlockMethodIgnoresSemicolonAtEndOfSimpleImplicitExpression()
        {
            RunTrailingSemicolonTest("foo");
        }

        [Fact]
        public void ParseBlockMethodParsesDottedIdentifiersAsImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar.baz");
        }

        [Fact]
        public void ParseBlockMethodIgnoresSemicolonAtEndOfDottedIdentifiers()
        {
            RunTrailingSemicolonTest("foo.bar.baz");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeDotAtEOFInImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar.", "foo.bar");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar.0", "foo.bar");
            ImplicitExpressionTest("foo.bar.</p>", "foo.bar");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeSemicolonAfterDot()
        {
            ImplicitExpressionTest("foo.bar.;", "foo.bar");
        }

        [Fact]
        public void ParseBlockMethodTerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar</p>", "foo.bar");
        }

        [Fact]
        public void ParseBlockProperlyParsesParenthesesAndBalancesThemInImplicitExpression()
        {
            ImplicitExpressionTest(@"foo().bar(""bi\""z"", 4)(""chained method; call"").baz(@""bo""""z"", '\'', () => { return 4; }, (4+5+new { foo = bar[4] }))");
        }

        [Fact]
        public void ParseBlockProperlyParsesBracketsAndBalancesThemInImplicitExpression()
        {
            ImplicitExpressionTest(@"foo.bar[4 * (8 + 7)][""fo\""o""].baz");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionAtHtmlEndTag()
        {
            ImplicitExpressionTest("foo().bar.baz</p>zoop", "foo().bar.baz");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionAtHtmlStartTag()
        {
            ImplicitExpressionTest("foo().bar.baz<p>zoop", "foo().bar.baz");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionBeforeDotIfDotNotFollowedByIdentifierStartCharacter()
        {
            ImplicitExpressionTest("foo().bar.baz.42", "foo().bar.baz");
        }

        [Fact]
        public void ParseBlockStopsBalancingParenthesesAtEOF()
        {
            ImplicitExpressionTest(
                "foo(()", "foo(()",
                acceptedCharacters: AcceptedCharactersInternal.Any,
                errors: RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                    new SourceLocation(4, 0, 4),
                    length: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionIfCloseParenFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo.bar() (baz)", "foo.bar()");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionIfIdentifierFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo .bar() (baz)", "foo");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionAtLastValidPointIfDotFollowedByWhitespace()
        {
            ImplicitExpressionTest("foo. bar() (baz)", "foo");
        }

        [Fact]
        public void ParseBlockOutputExpressionIfModuleTokenNotFollowedByBrace()
        {
            ImplicitExpressionTest("module.foo()");
        }

        private void RunTrailingSemicolonTest(string expr)
        {
            ParseBlockTest(SyntaxConstants.TransitionString + expr + ";",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.Code(expr)
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                               ));
        }
    }
}
