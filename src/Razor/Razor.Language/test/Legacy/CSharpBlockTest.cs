// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpBlockTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void NestedCodeBlockWithCSharpAt()
        {
            ParseBlockTest("{ if (true) { var val = @x; if (val != 3) { } } }");
        }

        [Fact]
        public void NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseBlockTest("if (true) { @if(false) { <div>@something.</div> } }");
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBrackets()
        {
            // BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments
            SingleSpanBlockTest(@"if(foo) {
    // bar } "" baz '
    zoop();
}");
        }

        [Fact]
        public void NestedCodeBlockWithAtDoesntCauseError()
        {
            ParseBlockTest("if (true) { @if(false) { } }");
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideBlockComments()
        {
            SingleSpanBlockTest(
                @"if(foo) {
    /* bar } "" */ ' baz } '
    zoop();
}");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword
            SingleSpanBlockTest(
                "for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword
            SingleSpanBlockTest(
                "foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword
            SingleSpanBlockTest(
                "while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesIfFirstIdentifierIsUsingFollowedByParen()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen
            SingleSpanBlockTest(
                "using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SupportsUsingsNestedWithinOtherBlocks()
        {
            SingleSpanBlockTest(
                "if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches
            SingleSpanBlockTest(
                "if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void AllowsEmptyBlockStatement()
        {
            SingleSpanBlockTest("if(false) { }");
        }

        [Fact]
        public void TerminatesParenBalancingAtEOF()
        {
            ImplicitExpressionTest("Html.En(code()");
        }

        [Fact]
        public void SupportsBlockCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } ", " else { baz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenIfAndElseIfClause()
        {
            RunRazorCommentBetweenClausesTest("if(foo) { bar(); } ", " else if(bar) { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }");
        }

        [Fact]
        public void ParsesElseIfBranchesOfIfStatement()
        {
            const string ifStatement = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string document = ifStatement + elseIfBranch;

            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParsesMultipleElseIfBranchesOfIfStatement()
        {
            const string ifStatement = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string document = ifStatement + elseIfBranch + elseIfBranch + elseIfBranch + elseIfBranch;
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParsesMultipleElseIfBranchesOfIfStatementFollowedByOneElseBranch()
        {
            const string ifStatement = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string elseBranch = @" else { Debug.WriteLine(@""bar } baz""); }";
            const string document = ifStatement + elseIfBranch + elseIfBranch + elseBranch;

            SingleSpanBlockTest(document);
        }

        [Fact]
        public void StopsParsingCodeAfterElseBranch()
        {
            const string ifStatement = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string elseBranch = @" else { Debug.WriteLine(@""bar } baz""); }";
            const string document = ifStatement + elseIfBranch + elseBranch + elseIfBranch;

            ParseBlockTest(document);
        }

        [Fact]
        public void StopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";

            SingleSpanBlockTest(document);
        }

        [Fact]
        public void AcceptsElseIfWithNoCondition()
        {
            // We don't want to be a full C# parser - If the else if is missing it's condition, the C# compiler
            // can handle that, we have all the info we need to keep parsing
            const string ifBranch = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if { foo(); }";
            const string document = ifBranch + elseIfBranch;

            SingleSpanBlockTest(document);
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlock()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while(foo != bar);");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar)");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            SingleSpanBlockTest("do { var foo = bar; } while");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while;");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            SingleSpanBlockTest("do { var foo = bar; } narf;");
        }

        [Fact]
        public void SupportsBlockCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } /* Foo */ /* Bar */ while(true);");
        }

        [Fact]
        public void SupportsLineCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(@"do { var foo = bar; }
// Foo
// Bar
while(true);");
        }

        [Fact]
        public void SupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest(
                "do { var foo = bar; } ", " while(true);");
        }

        [Fact]
        public void CorrectlyParsesMarkupInDoWhileBlock()
        {
            ParseBlockTest("@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsSwitchKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsSwitchKeyword
            SingleSpanBlockTest(@"switch(foo) {
    case 0:
        break;
    case 1:
        {
            break;
        }
    case 2:
        return;
    default:
        return;
}");
        }

        [Fact]
        public void ThenBalancesBracesIfFirstIdentifierIsLockKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsLockKeyword
            SingleSpanBlockTest(
                "lock(foo) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void HasErrorsIfNamespaceImportMissingSemicolon()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz");
        }

        [Fact]
        public void HasErrorsIfNamespaceAliasMissingSemicolon()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz = FooBarBaz");
        }

        [Fact]
        public void ParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz;");
        }

        [Fact]
        public void DoesntCaptureWhitespaceAfterUsing()
        {
            ParseBlockTest("using Foo   ");
        }

        [Fact]
        public void CapturesNewlineAfterUsing()
        {
            ParseBlockTest($"using Foo{Environment.NewLine}");
        }

        [Fact]
        public void ParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseBlockTest(
                "using FooBarBaz = FooBarBaz;");
        }

        [Fact]
        public void TerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            SingleSpanBlockTest("using                    ");
        }

        [Fact]
        public void TerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { // foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void TerminatesBlockCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { /* foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void TerminatesSingleSlashAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { / foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void SupportsBlockCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ finally { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } ", " finally { biz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(
                "try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest(
                "try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenTryAndCatchClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); }", " catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
finally { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsTryStatementWithNoAdditionalClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinTryClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithOneCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinCatchClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithMultipleCatchClause()
        {
            SingleSpanBlockTest(
                "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void SupportsExceptionLessCatchClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinAdditionalCatchClauses()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithFinallyClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } finally { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinFinallyClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } finally {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void StopsParsingCatchClausesAfterFinallyBlock()
        {
            var content = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(content + " catch(Foo Bar Baz) { }");
        }

        [Fact]
        public void DoesNotAllowMultipleFinallyBlocks()
        {
            var content = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(content + " finally { }");
        }

        [Fact]
        public void AcceptsTrailingDotIntoImplicitExpressionWhenEmbeddedInCode()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo. }");
        }

        [Fact]
        public void ParsesExpressionOnSwitchCharacterFollowedByOpenParen()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @(foo + bar) }");
        }

        [Fact]
        public void ParsesExpressionOnSwitchCharacterFollowedByIdentifierStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo[4].bar() }");
        }

        [Fact]
        public void TreatsDoubleAtSignAsEscapeSequenceIfAtStatementStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@class.Foo() }");
        }

        [Fact]
        public void TreatsAtSignsAfterFirstPairAsPartOfCSharpStatement()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@@@class.Foo() }");
        }

        [Fact]
        public void DoesNotParseOnSwitchCharacterNotFollowedByOpenAngleOrColon()
        {
            // ParseBlockDoesNotParseMarkupStatementOrExpressionOnSwitchCharacterNotFollowedByOpenAngleOrColon
            // Arrange
            ParseBlockTest("if(foo) { @\"Foo\".ToString(); }");
        }

        [Fact]
        public void ParsersCanNestRecursively()
        {
            // Arrange
            ParseBlockTest("foreach(var c in db.Categories) {" + Environment.NewLine
                         + "            <div>" + Environment.NewLine
                         + "                <h1>@c.Name</h1>" + Environment.NewLine
                         + "                <ul>" + Environment.NewLine
                         + "                    @foreach(var p in c.Products) {" + Environment.NewLine
                         + "                        <li><a href=\"@Html.ActionUrl(\"Products\", \"Detail\", new { id = p.Id })\">@p.Name</a></li>" + Environment.NewLine
                         + "                    }" + Environment.NewLine
                         + "                </ul>" + Environment.NewLine
                         + "            </div>" + Environment.NewLine
                         + "        }");
        }

        [Fact]
        public void WithDoubleTransitionInAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionAtEndOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc@@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionAtBeginningOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@def' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionBetweenAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc @@ def' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionWithExpressionBlock_DoesNotThrow()
        {
            var input = "{<span foo='@@@(2+3)' bar='@(2+3)@@@DateTime.Now' baz='@DateTime.Now@@' bat='@DateTime.Now @@' zoo='@@@DateTime.Now' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionInEmail_DoesNotThrow()
        {
            var input = "{<span foo='abc@def.com abc@@def.com @@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransitionInRegex_DoesNotThrow()
        {
            var input = @"{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void WithDoubleTransition_EndOfFile_Throws()
        {
            ParseBlockTest("{<span foo='@@");
        }

        [Fact]
        public void WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            ParseBlockTest("{<span foo='@ @' />}");
        }

        private void RunRazorCommentBetweenClausesTest(string preComment, string postComment, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            ParseBlockTest(preComment + "@* Foo *@ @* Bar *@" + postComment);
        }

        private void RunSimpleWrappedMarkupTest(string prefix, string markup, string suffix)
        {
            ParseBlockTest(prefix + markup + suffix);
        }
    }
}
