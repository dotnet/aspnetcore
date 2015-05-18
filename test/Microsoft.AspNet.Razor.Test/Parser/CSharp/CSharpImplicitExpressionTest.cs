// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpImplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        private const string TestExtraKeyword = "model";

        public override ParserBase CreateCodeParser()
        {
            return new CSharpCodeParser();
        }

        public static TheoryData NullConditionalOperatorData_Bracket
        {
            get
            {
                var noErrors = new RazorError[0];
                Func<int, RazorError[]> missingEndParenError = (index) =>
                    new RazorError[1]
                    {
                        new RazorError("An opening \"(\" is missing the corresponding closing \")\".", index, 0, index)
                    };
                Func<int, RazorError[]> missingEndBracketError = (index) =>
                    new RazorError[1]
                    {
                        new RazorError("An opening \"[\" is missing the corresponding closing \"]\".", index, 0, index)
                    };

                // implicitExpression, expectedImplicitExpression, acceptedCharacters, expectedErrors
                return new TheoryData<string, string, AcceptedCharacters, RazorError[]>
                {
                    { "val??[", "val", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val??[0", "val", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[", "val?[", AcceptedCharacters.Any, missingEndBracketError(5) },
                    { "val?(", "val", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[more", "val?[more", AcceptedCharacters.Any, missingEndBracketError(5) },
                    { "val?[0]", "val?[0]", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[<p>", "val?[", AcceptedCharacters.Any, missingEndBracketError(5) },
                    { "val?[more.<p>", "val?[more.", AcceptedCharacters.Any, missingEndBracketError(5) },
                    { "val??[more<p>", "val", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[-1]?", "val?[-1]", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[abc]?[def", "val?[abc]?[def", AcceptedCharacters.Any, missingEndBracketError(11) },
                    { "val?[abc]?[2]", "val?[abc]?[2]", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[abc]?.more?[def]", "val?[abc]?.more?[def]", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[abc]?.more?.abc", "val?[abc]?.more?.abc", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[null ?? true]", "val?[null ?? true]", AcceptedCharacters.NonWhiteSpace, noErrors },
                    { "val?[abc?.gef?[-1]]", "val?[abc?.gef?[-1]]", AcceptedCharacters.NonWhiteSpace, noErrors },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullConditionalOperatorData_Bracket))]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket(
            string implicitExpresison,
            string expectedImplicitExpression,
            AcceptedCharacters acceptedCharacters,
            RazorError[] expectedErrors)
        {
            // Act & Assert
            ImplicitExpressionTest(
                implicitExpresison,
                expectedImplicitExpression,
                acceptedCharacters,
                expectedErrors);
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
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
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
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                           new RazorError(
                               RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("/"),
                               new SourceLocation(1, 0, 1)));
        }

        [Fact]
        public void ParseBlockOutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseBlockTest("@",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                           new RazorError(
                               RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                               new SourceLocation(1, 0, 1)));
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
            ImplicitExpressionTest("foo(()", "foo(()",
                                   acceptedCharacters: AcceptedCharacters.Any,
                                   errors: new RazorError(RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"), new SourceLocation(4, 0, 4)));
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
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }
    }
}
