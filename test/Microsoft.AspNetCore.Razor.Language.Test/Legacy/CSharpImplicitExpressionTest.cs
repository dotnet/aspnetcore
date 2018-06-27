// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpImplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        public CSharpImplicitExpressionTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket1()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket2()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[0");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket3()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket4()
        {
            // Act & Assert
            ImplicitExpressionTest("val?(");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket5()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[more");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket6()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[0]");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket7()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket8()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[more.<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket9()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[more<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket10()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[-1]?");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket11()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?[def");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket12()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?[2]");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket13()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?.more?[def]");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket14()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?.more?.abc");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket15()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[null ?? true]");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Bracket16()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc?.gef?[-1]]");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot1()
        {
            // Act & Assert
            ImplicitExpressionTest("val?");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot2()
        {
            // Act & Assert
            ImplicitExpressionTest("val??");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot3()
        {
            // Act & Assert
            ImplicitExpressionTest("val??more");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot4()
        {
            // Act & Assert
            ImplicitExpressionTest("val?!");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot5()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot6()
        {
            // Act & Assert
            ImplicitExpressionTest("val??.");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot7()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.(abc)");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot8()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot9()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot10()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot11()
        {
            // Act & Assert
            ImplicitExpressionTest("val??.more<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot12()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more(false)?.<p>");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot13()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more(false)?.abc");
        }

        [Fact]
        public void ParseBlockMethodParsesNullConditionalOperatorImplicitExpression_Dot14()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more(null ?? true)?.abc");
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseBlockTest("if (true) { @foo }");
        }

        [Fact]
        public void ParseBlockAcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ImplicitExpressionTest("हळूँजद॔.");
        }

        [Fact]
        public void ParseBlockOutputsZeroLengthCodeSpanIfInvalidCharacterFollowsTransition()
        {
            ParseBlockTest("@/");
        }

        [Fact]
        public void ParseBlockOutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseBlockTest("@");
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
            ImplicitExpressionTest("foo ;");
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
            ImplicitExpressionTest("foo.bar.");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression1()
        {
            ImplicitExpressionTest("foo.bar.0");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression2()
        {
            ImplicitExpressionTest("foo.bar.</p>");
        }

        [Fact]
        public void ParseBlockMethodDoesNotIncludeSemicolonAfterDot()
        {
            ImplicitExpressionTest("foo.bar.;");
        }

        [Fact]
        public void ParseBlockMethodTerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar</p>");
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
            ImplicitExpressionTest("foo().bar.baz</p>zoop");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionAtHtmlStartTag()
        {
            ImplicitExpressionTest("foo().bar.baz<p>zoop");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionBeforeDotIfDotNotFollowedByIdentifierStartCharacter()
        {
            ImplicitExpressionTest("foo().bar.baz.42");
        }

        [Fact]
        public void ParseBlockStopsBalancingParenthesesAtEOF()
        {
            ImplicitExpressionTest("foo(()");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionIfCloseParenFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo.bar() (baz)");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionIfIdentifierFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo .bar() (baz)");
        }

        [Fact]
        public void ParseBlockTerminatesImplicitExpressionAtLastValidPointIfDotFollowedByWhitespace()
        {
            ImplicitExpressionTest("foo. bar() (baz)");
        }

        [Fact]
        public void ParseBlockOutputExpressionIfModuleTokenNotFollowedByBrace()
        {
            ImplicitExpressionTest("module.foo()");
        }

        private void RunTrailingSemicolonTest(string expr)
        {
            ParseBlockTest(SyntaxConstants.TransitionString + expr + ";");
        }
    }
}
