// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpImplicitExpressionTest : ParserTestBase
    {
        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket1()
        {
            // Act & Assert
            ParseDocumentTest("@val??[");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket2()
        {
            // Act & Assert
            ParseDocumentTest("@val??[0");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket3()
        {
            // Act & Assert
            ParseDocumentTest("@val?[");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket4()
        {
            // Act & Assert
            ParseDocumentTest("@val?(");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket5()
        {
            // Act & Assert
            ParseDocumentTest("@val?[more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket6()
        {
            // Act & Assert
            ParseDocumentTest("@val?[0]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket7()
        {
            // Act & Assert
            ParseDocumentTest("@val?[<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket8()
        {
            // Act & Assert
            ParseDocumentTest("@val?[more.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket9()
        {
            // Act & Assert
            ParseDocumentTest("@val??[more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket10()
        {
            // Act & Assert
            ParseDocumentTest("@val?[-1]?");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket11()
        {
            // Act & Assert
            ParseDocumentTest("@val?[abc]?[def");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket12()
        {
            // Act & Assert
            ParseDocumentTest("@val?[abc]?[2]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket13()
        {
            // Act & Assert
            ParseDocumentTest("@val?[abc]?.more?[def]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket14()
        {
            // Act & Assert
            ParseDocumentTest("@val?[abc]?.more?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket15()
        {
            // Act & Assert
            ParseDocumentTest("@val?[null ?? true]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket16()
        {
            // Act & Assert
            ParseDocumentTest("@val?[abc?.gef?[-1]]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot1()
        {
            // Act & Assert
            ParseDocumentTest("@val?");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot2()
        {
            // Act & Assert
            ParseDocumentTest("@val??");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot3()
        {
            // Act & Assert
            ParseDocumentTest("@val??more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot4()
        {
            // Act & Assert
            ParseDocumentTest("@val?!");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot5()
        {
            // Act & Assert
            ParseDocumentTest("@val?.");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot6()
        {
            // Act & Assert
            ParseDocumentTest("@val??.");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot7()
        {
            // Act & Assert
            ParseDocumentTest("@val?.(abc)");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot8()
        {
            // Act & Assert
            ParseDocumentTest("@val?.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot9()
        {
            // Act & Assert
            ParseDocumentTest("@val?.more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot10()
        {
            // Act & Assert
            ParseDocumentTest("@val?.more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot11()
        {
            // Act & Assert
            ParseDocumentTest("@val??.more<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot12()
        {
            // Act & Assert
            ParseDocumentTest("@val?.more(false)?.<p>");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot13()
        {
            // Act & Assert
            ParseDocumentTest("@val?.more(false)?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot14()
        {
            // Act & Assert
            ParseDocumentTest("@val?.more(null ?? true)?.abc");
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseDocumentTest("if (true) { @foo }");
        }

        [Fact]
        public void AcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ParseDocumentTest("@हळूँजद॔.");
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfInvalidCharacterFollowsTransition()
        {
            ParseDocumentTest("@/");
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseDocumentTest("@");
        }

        [Fact]
        public void SupportsSlashesWithinComplexImplicitExpressions()
        {
            ParseDocumentTest("@DataGridColumn.Template(\"Years of Service\", e => (int)Math.Round((DateTime.Now - dt).TotalDays / 365))");
        }

        [Fact]
        public void ParsesSingleIdentifierAsImplicitExpression()
        {
            ParseDocumentTest("@foo");
        }

        [Fact]
        public void DoesNotAcceptSemicolonIfExpressionTerminatedByWhitespace()
        {
            ParseDocumentTest("@foo ;");
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfSimpleImplicitExpression()
        {
            ParseDocumentTest("@foo;");
        }

        [Fact]
        public void ParsesDottedIdentifiersAsImplicitExpression()
        {
            ParseDocumentTest("@foo.bar.baz");
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfDottedIdentifiers()
        {
            ParseDocumentTest("@foo.bar.baz;");
        }

        [Fact]
        public void DoesNotIncludeDotAtEOFInImplicitExpression()
        {
            ParseDocumentTest("@foo.bar.");
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr1()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression1
            ParseDocumentTest("@foo.bar.0");
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr2()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression2
            ParseDocumentTest("@foo.bar.</p>");
        }

        [Fact]
        public void DoesNotIncludeSemicolonAfterDot()
        {
            ParseDocumentTest("@foo.bar.;");
        }

        [Fact]
        public void TerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpr()
        {
            // ParseBlockMethodTerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpression
            ParseDocumentTest("@foo.bar</p>");
        }

        [Fact]
        public void ProperlyParsesParenthesesAndBalancesThemInImplicitExpression()
        {
            ParseDocumentTest(@"@foo().bar(""bi\""z"", 4)(""chained method; call"").baz(@""bo""""z"", '\'', () => { return 4; }, (4+5+new { foo = bar[4] }))");
        }

        [Fact]
        public void ProperlyParsesBracketsAndBalancesThemInImplicitExpression()
        {
            ParseDocumentTest(@"@foo.bar[4 * (8 + 7)][""fo\""o""].baz");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlEndTag()
        {
            ParseDocumentTest("@foo().bar.baz</p>zoop");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlStartTag()
        {
            ParseDocumentTest("@foo().bar.baz<p>zoop");
        }

        [Fact]
        public void TerminatesImplicitExprBeforeDotIfDotNotFollowedByIdentifierStartChar()
        {
            // ParseBlockTerminatesImplicitExpressionBeforeDotIfDotNotFollowedByIdentifierStartCharacter
            ParseDocumentTest("@foo().bar.baz.42");
        }

        [Fact]
        public void StopsBalancingParenthesesAtEOF()
        {
            ParseDocumentTest("@foo(()");
        }

        [Fact]
        public void TerminatesImplicitExpressionIfCloseParenFollowedByAnyWhiteSpace()
        {
            ParseDocumentTest("@foo.bar() (baz)");
        }

        [Fact]
        public void TerminatesImplicitExpressionIfIdentifierFollowedByAnyWhiteSpace()
        {
            ParseDocumentTest("@foo .bar() (baz)");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtLastValidPointIfDotFollowedByWhitespace()
        {
            ParseDocumentTest("@foo. bar() (baz)");
        }

        [Fact]
        public void OutputExpressionIfModuleTokenNotFollowedByBrace()
        {
            ParseDocumentTest("@module.foo()");
        }
    }
}
