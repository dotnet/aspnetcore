// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpBlockTest : ParserTestBase
    {
        [Fact]
        public void CSharpBlock_SingleLineControlFlowStatement_Error()
        {
            ParseDocumentTest(
@"@{
    var time = DateTime.Now;
    if (time.ToBinary() % 2 == 0) <p>The time: @time</p>

    if (time.ToBinary() %3 == 0)
        // For some reason we want to render the time now?
        <p>The confusing time: @time</p>

    if (time.ToBinary() % 4 == 0)
        @: <p>The time: @time</p>

    if (time.ToBinary() % 5 == 0) @@SomeGitHubUserName <strong>Hi!</strong>
}");
        }

        [Fact]
        public void CSharpBlock_SingleLineControlFlowStatement()
        {
            ParseDocumentTest(
@"@{
    var time = DateTime.Now;
    if (time.ToBinary() % 2 == 0) @time
}");
        }

        [Fact]
        public void LocalFunctionsWithRazor()
        {
            ParseDocumentTest(
@"@{
    void Foo() 
    {
        var time = DateTime.Now
        <strong>Hello the time is @time</strong>
    }
}");
        }

        [Fact]
        public void LocalFunctionsWithGenerics()
        {
            ParseDocumentTest(
@"@{
    void Foo()
    {
        <strong>Hello the time is @{ DisplayCount(new List<string>()); }</strong>
    }

    void DisplayCount<T>(List<T> something)
    {
        <text>The count is something.Count</text>
    }
}");
        }

        [Fact]
        public void NestedCodeBlockWithCSharpAt()
        {
            ParseDocumentTest("@{ if (true) { var val = @x; if (val != 3) { } } }");
        }

        [Fact]
        public void NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseDocumentTest("@if (true) { @if(false) { <div>@something.</div> } }");
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBrackets()
        {
            // BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments
            ParseDocumentTest(@"@if(foo) {
    // bar } "" baz '
    zoop();
}");
        }

        [Fact]
        public void NestedCodeBlockWithAtDoesntCauseError()
        {
            ParseDocumentTest("@if (true) { @if(false) { } }");
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideBlockComments()
        {
            ParseDocumentTest(
                @"@if(foo) {
    /* bar } "" */ ' baz } '
    zoop();
}");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword
            ParseDocumentTest(
                "@for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword
            ParseDocumentTest(
                "@foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword
            ParseDocumentTest(
                "@while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SkipsExprThenBalancesIfFirstIdentifierIsUsingFollowedByParen()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen
            ParseDocumentTest(
                "@using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void SupportsUsingsNestedWithinOtherBlocks()
        {
            ParseDocumentTest(
                "@if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches
            ParseDocumentTest(
                "@if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void AllowsEmptyBlockStatement()
        {
            ParseDocumentTest("@if(false) { }");
        }

        [Fact]
        public void TerminatesParenBalancingAtEOF()
        {
            ParseDocumentTest("@Html.En(code()");
        }

        [Fact]
        public void SupportsBlockCommentBetweenIfAndElseClause()
        {
            ParseDocumentTest(
                "@if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "@if(foo) { bar(); } ", " else { baz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenElseIfAndElseClause()
        {
            ParseDocumentTest(
                "@if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "@if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenIfAndElseIfClause()
        {
            ParseDocumentTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenIfAndElseIfClause()
        {
            RunRazorCommentBetweenClausesTest("@if(foo) { bar(); } ", " else if(bar) { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenIfAndElseClause()
        {
            ParseDocumentTest(@"@if(foo) { bar(); }
// Foo
// Bar
else { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenElseIfAndElseClause()
        {
            ParseDocumentTest(@"@if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenIfAndElseIfClause()
        {
            ParseDocumentTest(@"@if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }");
        }

        [Fact]
        public void ParsesElseIfBranchesOfIfStatement()
        {
            const string ifStatement = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string document = ifStatement + elseIfBranch;

            ParseDocumentTest(document);
        }

        [Fact]
        public void ParsesMultipleElseIfBranchesOfIfStatement()
        {
            const string ifStatement = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string document = ifStatement + elseIfBranch + elseIfBranch + elseIfBranch + elseIfBranch;
            ParseDocumentTest(document);
        }

        [Fact]
        public void ParsesMultipleElseIfBranchesOfIfStatementFollowedByOneElseBranch()
        {
            const string ifStatement = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string elseBranch = @" else { Debug.WriteLine(@""bar } baz""); }";
            const string document = ifStatement + elseIfBranch + elseIfBranch + elseBranch;

            ParseDocumentTest(document);
        }

        [Fact]
        public void StopsParsingCodeAfterElseBranch()
        {
            const string ifStatement = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""bar } baz"");
}";
            const string elseBranch = @" else { Debug.WriteLine(@""bar } baz""); }";
            const string document = ifStatement + elseIfBranch + elseBranch + elseIfBranch;

            ParseDocumentTest(document);
        }

        [Fact]
        public void StopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";

            ParseDocumentTest(document);
        }

        [Fact]
        public void AcceptsElseIfWithNoCondition()
        {
            // We don't want to be a full C# parser - If the else if is missing it's condition, the C# compiler
            // can handle that, we have all the info we need to keep parsing
            const string ifBranch = @"@if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";
            const string elseIfBranch = @" else if { foo(); }";
            const string document = ifBranch + elseIfBranch;

            ParseDocumentTest(document);
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlock()
        {
            ParseDocumentTest(
                "@do { var foo = bar; } while(foo != bar);");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            ParseDocumentTest("@do { var foo = bar; } while(foo != bar)");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            ParseDocumentTest("@do { var foo = bar; } while");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            ParseDocumentTest(
                "@do { var foo = bar; } while;");
        }

        [Fact]
        public void CorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            ParseDocumentTest("@do { var foo = bar; } narf;");
        }

        [Fact]
        public void SupportsBlockCommentBetweenDoAndWhileClause()
        {
            ParseDocumentTest(
                "@do { var foo = bar; } /* Foo */ /* Bar */ while(true);");
        }

        [Fact]
        public void SupportsLineCommentBetweenDoAndWhileClause()
        {
            ParseDocumentTest(@"@do { var foo = bar; }
// Foo
// Bar
while(true);");
        }

        [Fact]
        public void SupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest(
                "@do { var foo = bar; } ", " while(true);");
        }

        [Fact]
        public void CorrectlyParsesMarkupInDoWhileBlock()
        {
            ParseDocumentTest("@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);");
        }

        [Fact]
        public void SkipsExprThenBalancesBracesIfFirstIdentifierIsSwitchKeyword()
        {
            // ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsSwitchKeyword
            ParseDocumentTest(@"@switch(foo) {
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
            ParseDocumentTest(
                "@lock(foo) { Debug.WriteLine(@\"foo } bar\"); }");
        }

        [Fact]
        public void HasErrorsIfNamespaceImportMissingSemicolon()
        {
            ParseDocumentTest(
                "@using Foo.Bar.Baz");
        }

        [Fact]
        public void HasErrorsIfNamespaceAliasMissingSemicolon()
        {
            ParseDocumentTest(
                "@using Foo.Bar.Baz = FooBarBaz");
        }

        [Fact]
        public void ParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseDocumentTest(
                "@using Foo.Bar.Baz;");
        }

        [Fact]
        public void DoesntCaptureWhitespaceAfterUsing()
        {
            ParseDocumentTest("@using Foo   ");
        }

        [Fact]
        public void CapturesNewlineAfterUsing()
        {
            ParseDocumentTest($"@using Foo{Environment.NewLine}");
        }

        [Fact]
        public void ParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            ParseDocumentTest(
                "@using FooBarBaz = FooBarBaz;");
        }

        [Fact]
        public void TerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            ParseDocumentTest("@using                    ");
        }

        [Fact]
        public void TerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "@foreach(var f in Foo) { // foo bar baz";
            ParseDocumentTest(document);
        }

        [Fact]
        public void TerminatesBlockCommentAtEndOfFile()
        {
            const string document = "@foreach(var f in Foo) { /* foo bar baz";
            ParseDocumentTest(document);
        }

        [Fact]
        public void TerminatesSingleSlashAtEndOfFile()
        {
            const string document = "@foreach(var f in Foo) { / foo bar baz";
            ParseDocumentTest(document);
        }

        [Fact]
        public void SupportsBlockCommentBetweenTryAndFinallyClause()
        {
            ParseDocumentTest("@try { bar(); } /* Foo */ /* Bar */ finally { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("@try { bar(); } ", " finally { biz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            ParseDocumentTest(
                "@try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest(
                "@try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }");
        }

        [Fact]
        public void SupportsBlockCommentBetweenTryAndCatchClause()
        {
            ParseDocumentTest("@try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsRazorCommentBetweenTryAndCatchClause()
        {
            RunRazorCommentBetweenClausesTest("@try { bar(); }", " catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenTryAndFinallyClause()
        {
            ParseDocumentTest(@"@try { bar(); }
// Foo
// Bar
finally { baz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenCatchAndFinallyClause()
        {
            ParseDocumentTest(@"@try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }");
        }

        [Fact]
        public void SupportsLineCommentBetweenTryAndCatchClause()
        {
            ParseDocumentTest(@"@try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }");
        }

        [Fact]
        public void SupportsTryStatementWithNoAdditionalClauses()
        {
            ParseDocumentTest("@try { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinTryClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "@try {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithOneCatchClause()
        {
            ParseDocumentTest("@try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinCatchClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "@try { var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithMultipleCatchClause()
        {
            ParseDocumentTest(
                "@try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }");
        }

        [Fact]
        public void SupportsExceptionLessCatchClauses()
        {
            ParseDocumentTest("@try { var foo = new { } } catch { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinAdditionalCatchClauses()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "@try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void SupportsTryStatementWithFinallyClause()
        {
            ParseDocumentTest("@try { var foo = new { } } finally { var foo = new { } }");
        }

        [Fact]
        public void SupportsMarkupWithinFinallyClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "@try { var foo = new { } } finally {",
                markup: " <p>Foo</p> ",
                suffix: "}");
        }

        [Fact]
        public void StopsParsingCatchClausesAfterFinallyBlock()
        {
            var content = "@try { var foo = new { } } finally { var foo = new { } }";
            ParseDocumentTest(content + " catch(Foo Bar Baz) { }");
        }

        [Fact]
        public void DoesNotAllowMultipleFinallyBlocks()
        {
            var content = "@try { var foo = new { } } finally { var foo = new { } }";
            ParseDocumentTest(content + " finally { }");
        }

        [Fact]
        public void AcceptsTrailingDotIntoImplicitExpressionWhenEmbeddedInCode()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @foo. }");
        }

        [Fact]
        public void ParsesExpressionOnSwitchCharacterFollowedByOpenParen()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @(foo + bar) }");
        }

        [Fact]
        public void ParsesExpressionOnSwitchCharacterFollowedByIdentifierStart()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @foo[4].bar() }");
        }

        [Fact]
        public void TreatsDoubleAtSignAsEscapeSequenceIfAtStatementStart()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @@class.Foo() }");
        }

        [Fact]
        public void TreatsAtSignsAfterFirstPairAsPartOfCSharpStatement()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @@@@class.Foo() }");
        }

        [Fact]
        public void DoesNotParseOnSwitchCharacterNotFollowedByOpenAngleOrColon()
        {
            // ParseBlockDoesNotParseMarkupStatementOrExpressionOnSwitchCharacterNotFollowedByOpenAngleOrColon
            // Arrange
            ParseDocumentTest("@if(foo) { @\"Foo\".ToString(); }");
        }

        [Fact]
        public void ParsersCanNestRecursively()
        {
            // Arrange
            ParseDocumentTest("@foreach(var c in db.Categories) {" + Environment.NewLine
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
            var input = "@{<span foo='@@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionAtEndOfAttributeValue_DoesNotThrow()
        {
            var input = "@{<span foo='abc@@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionAtBeginningOfAttributeValue_DoesNotThrow()
        {
            var input = "@{<span foo='@@def' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionBetweenAttributeValue_DoesNotThrow()
        {
            var input = "@{<span foo='abc @@ def' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionWithExpressionBlock_DoesNotThrow()
        {
            var input = "@{<span foo='@@@(2+3)' bar='@(2+3)@@@DateTime.Now' baz='@DateTime.Now@@' bat='@DateTime.Now @@' zoo='@@@DateTime.Now' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionInEmail_DoesNotThrow()
        {
            var input = "@{<span foo='abc@def.com abc@@def.com @@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransitionInRegex_DoesNotThrow()
        {
            var input = @"@{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void WithDoubleTransition_EndOfFile_Throws()
        {
            ParseDocumentTest("@{<span foo='@@");
        }

        [Fact]
        public void WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            ParseDocumentTest("@{<span foo='@ @' />}");
        }

        private void RunRazorCommentBetweenClausesTest(string preComment, string postComment, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            ParseDocumentTest(preComment + "@* Foo *@ @* Bar *@" + postComment);
        }

        private void RunSimpleWrappedMarkupTest(string prefix, string markup, string suffix)
        {
            ParseDocumentTest(prefix + markup + suffix);
        }
    }
}
