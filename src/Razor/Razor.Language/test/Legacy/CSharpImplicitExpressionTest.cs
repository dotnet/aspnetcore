// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpImplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket1()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket2()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[0");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket3()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket4()
        {
            // Act & Assert
            ImplicitExpressionTest("val?(");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket5()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket6()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[0]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket7()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket8()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[more.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket9()
        {
            // Act & Assert
            ImplicitExpressionTest("val??[more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket10()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[-1]?");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket11()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?[def");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket12()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?[2]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket13()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?.more?[def]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket14()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc]?.more?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket15()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[null ?? true]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket16()
        {
            // Act & Assert
            ImplicitExpressionTest("val?[abc?.gef?[-1]]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot1()
        {
            // Act & Assert
            ImplicitExpressionTest("val?");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot2()
        {
            // Act & Assert
            ImplicitExpressionTest("val??");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot3()
        {
            // Act & Assert
            ImplicitExpressionTest("val??more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot4()
        {
            // Act & Assert
            ImplicitExpressionTest("val?!");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot5()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot6()
        {
            // Act & Assert
            ImplicitExpressionTest("val??.");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot7()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.(abc)");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot8()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot9()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot10()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot11()
        {
            // Act & Assert
            ImplicitExpressionTest("val??.more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot12()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more(false)?.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot13()
        {
            // Act & Assert
            ImplicitExpressionTest("val?.more(false)?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot14()
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
        public void AcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ImplicitExpressionTest("हळूँजद॔.");
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfInvalidCharacterFollowsTransition()
        {
            ParseBlockTest("@/");
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseBlockTest("@");
        }

        [Fact]
        public void SupportsSlashesWithinComplexImplicitExpressions()
        {
            ImplicitExpressionTest("DataGridColumn.Template(\"Years of Service\", e => (int)Math.Round((DateTime.Now - dt).TotalDays / 365))");
        }

        [Fact]
        public void ParsesSingleIdentifierAsImplicitExpression()
        {
            ImplicitExpressionTest("foo");
        }

        [Fact]
        public void DoesNotAcceptSemicolonIfExpressionTerminatedByWhitespace()
        {
            ImplicitExpressionTest("foo ;");
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfSimpleImplicitExpression()
        {
            RunTrailingSemicolonTest("foo");
        }

        [Fact]
        public void ParsesDottedIdentifiersAsImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar.baz");
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfDottedIdentifiers()
        {
            RunTrailingSemicolonTest("foo.bar.baz");
        }

        [Fact]
        public void DoesNotIncludeDotAtEOFInImplicitExpression()
        {
            ImplicitExpressionTest("foo.bar.");
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr1()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression1
            ImplicitExpressionTest("foo.bar.0");
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr2()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression2
            ImplicitExpressionTest("foo.bar.</p>");
        }

        [Fact]
        public void DoesNotIncludeSemicolonAfterDot()
        {
            ImplicitExpressionTest("foo.bar.;");
        }

        [Fact]
        public void TerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpr()
        {
            // ParseBlockMethodTerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpression
            ImplicitExpressionTest("foo.bar</p>");
        }

        [Fact]
        public void ProperlyParsesParenthesesAndBalancesThemInImplicitExpression()
        {
            ImplicitExpressionTest(@"foo().bar(""bi\""z"", 4)(""chained method; call"").baz(@""bo""""z"", '\'', () => { return 4; }, (4+5+new { foo = bar[4] }))");
        }

        [Fact]
        public void ProperlyParsesBracketsAndBalancesThemInImplicitExpression()
        {
            ImplicitExpressionTest(@"foo.bar[4 * (8 + 7)][""fo\""o""].baz");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlEndTag()
        {
            ImplicitExpressionTest("foo().bar.baz</p>zoop");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlStartTag()
        {
            ImplicitExpressionTest("foo().bar.baz<p>zoop");
        }

        [Fact]
        public void TerminatesImplicitExprBeforeDotIfDotNotFollowedByIdentifierStartChar()
        {
            // ParseBlockTerminatesImplicitExpressionBeforeDotIfDotNotFollowedByIdentifierStartCharacter
            ImplicitExpressionTest("foo().bar.baz.42");
        }

        [Fact]
        public void StopsBalancingParenthesesAtEOF()
        {
            ImplicitExpressionTest("foo(()");
        }

        [Fact]
        public void TerminatesImplicitExpressionIfCloseParenFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo.bar() (baz)");
        }

        [Fact]
        public void TerminatesImplicitExpressionIfIdentifierFollowedByAnyWhiteSpace()
        {
            ImplicitExpressionTest("foo .bar() (baz)");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtLastValidPointIfDotFollowedByWhitespace()
        {
            ImplicitExpressionTest("foo. bar() (baz)");
        }

        [Fact]
        public void OutputExpressionIfModuleTokenNotFollowedByBrace()
        {
            ImplicitExpressionTest("module.foo()");
        }

        private void RunTrailingSemicolonTest(string expr)
        {
            ParseBlockTest(SyntaxConstants.TransitionString + expr + ";");
        }
    }
}
