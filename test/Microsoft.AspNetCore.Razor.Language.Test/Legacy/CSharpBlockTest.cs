// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpBlockTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlock_NestedCodeBlockWithCSharpAt()
        {
            ParseBlockTest("{ if (true) { var val = @x; if (val != 3) { } } }");
        }

        [Fact]
        public void ParseBlock_NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseBlockTest("if (true) { @if(false) { <div>@something.</div> } }");
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments()
        {
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
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            SingleSpanBlockTest(
                "for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            SingleSpanBlockTest(
                "foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            SingleSpanBlockTest(
                "while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen()
        {
            SingleSpanBlockTest(
                "using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockSupportsUsingsNestedWithinOtherBlocks()
        {
            SingleSpanBlockTest(
                "if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }");
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            SingleSpanBlockTest(
                "if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockAllowsEmptyBlockStatement()
        {
            SingleSpanBlockTest("if(false) { }");
        }

        [Fact]
        public void ParseBlockTerminatesParenBalancingAtEOF()
        {
            ImplicitExpressionTest("Html.En(code()");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } ", " else { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenIfAndElseIfClause()
        {
            RunRazorCommentBetweenClausesTest("if(foo) { bar(); } ", " else if(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockParsesElseIfBranchesOfIfStatement()
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
        public void ParseBlockParsesMultipleElseIfBranchesOfIfStatement()
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
        public void ParseBlockParsesMultipleElseIfBranchesOfIfStatementFollowedByOneElseBranch()
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
        public void ParseBlockStopsParsingCodeAfterElseBranch()
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
        public void ParseBlockStopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";

            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParseBlockAcceptsElseIfWithNoCondition()
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
        public void ParseBlockCorrectlyParsesDoWhileBlock()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while(foo != bar);");
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar)");
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            SingleSpanBlockTest("do { var foo = bar; } while");
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while;");
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            SingleSpanBlockTest("do { var foo = bar; } narf;");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } /* Foo */ /* Bar */ while(true);");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(@"do { var foo = bar; }
// Foo
// Bar
while(true);");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest(
                "do { var foo = bar; } ", " while(true);");
        }

        [Fact]
        public void ParseBlockCorrectlyParsesMarkupInDoWhileBlock()
        {
            ParseBlockTest("@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);");
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsSwitchKeyword()
        {
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
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsLockKeyword()
        {
            SingleSpanBlockTest(
                "lock(foo) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceImportMissingSemicolon()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz");
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceAliasMissingSemicolon()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz = FooBarBaz");
        }

        [Fact]
        public void ParseBlockParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseBlockTest(
                "using Foo.Bar.Baz;");
        }

        [Fact]
        public void ParseBlockDoesntCaptureWhitespaceAfterUsing()
        {
            ParseBlockTest("using Foo   ");
        }

        [Fact]
        public void ParseBlockCapturesNewlineAfterUsing()
        {
            ParseBlockTest($"using Foo{Environment.NewLine}");
        }

        [Fact]
        public void ParseBlockParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseBlockTest(
                "using FooBarBaz = FooBarBaz;");
        }

        [Fact]
        public void ParseBlockTerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            SingleSpanBlockTest("using                    ");
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { // foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParseBlockTerminatesBlockCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { /* foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParseBlockTerminatesSingleSlashAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { / foo bar baz";
            SingleSpanBlockTest(document);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ finally { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } ", " finally { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(
                "try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest(
                "try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenTryAndCatchClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); }", " catch(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
finally { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }");
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithNoAdditionalClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } }");
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinTryClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithOneCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinCatchClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithMultipleCatchClause()
        {
            SingleSpanBlockTest(
                "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void ParseBlockSupportsExceptionLessCatchClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch { var foo = new { } }");
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinAdditionalCatchClauses()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithFinallyClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } finally { var foo = new { } }");
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinFinallyClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } finally {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void ParseBlockStopsParsingCatchClausesAfterFinallyBlock()
        {
            var content = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(content + " catch(Foo Bar Baz) { }");
        }

        [Fact]
        public void ParseBlockDoesNotAllowMultipleFinallyBlocks()
        {
            var content = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(content + " finally { }");
        }

        [Fact]
        public void ParseBlockAcceptsTrailingDotIntoImplicitExpressionWhenEmbeddedInCode()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo. }");
        }

        [Fact]
        public void ParseBlockParsesExpressionOnSwitchCharacterFollowedByOpenParen()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @(foo + bar) }");
        }

        [Fact]
        public void ParseBlockParsesExpressionOnSwitchCharacterFollowedByIdentifierStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo[4].bar() }");
        }

        [Fact]
        public void ParseBlockTreatsDoubleAtSignAsEscapeSequenceIfAtStatementStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@class.Foo() }");
        }

        [Fact]
        public void ParseBlockTreatsAtSignsAfterFirstPairAsPartOfCSharpStatement()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@@@class.Foo() }");
        }

        [Fact]
        public void ParseBlockDoesNotParseMarkupStatementOrExpressionOnSwitchCharacterNotFollowedByOpenAngleOrColon()
        {
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
        public void ParseBlock_WithDoubleTransitionInAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionAtEndOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc@@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionAtBeginningOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@def' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionBetweenAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc @@ def' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionWithExpressionBlock_DoesNotThrow()
        {
            var input = "{<span foo='@@@(2+3)' bar='@(2+3)@@@DateTime.Now' baz='@DateTime.Now@@' bat='@DateTime.Now @@' zoo='@@@DateTime.Now' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionInEmail_DoesNotThrow()
        {
            var input = "{<span foo='abc@def.com abc@@def.com @@' />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransitionInRegex_DoesNotThrow()
        {
            var input = @"{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}";
            ParseBlockTest(input);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransition_EndOfFile_Throws()
        {
            ParseBlockTest("{<span foo='@@");
        }

        [Fact]
        public void ParseBlock_WithUnexpectedTransitionsInAttributeValue_Throws()
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
