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
            ParseBlockTest("{ if (true) { var val = @x; if (val != 3) { } } }",
                new StatementBlock(
                    Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                    Factory
                        .Code(" if (true) { var val = @x; if (val != 3) { } } ")
                        .AsStatement()
                        .Accepts(AcceptedCharactersInternal.Any)
                        .AutoCompleteWith(autoCompleteString: null, atEndOfSpan: false),
                    Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlock_NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseBlockTest("if (true) { @if(false) { <div>@something.</div> } }",
                new StatementBlock(
                    Factory.Code("if (true) { ").AsStatement(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if(false) {").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            BlockFactory.MarkupTagBlock("<div>", AcceptedCharactersInternal.None),
                            Factory.EmptyHtml(),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("something")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            Factory.Markup("."),
                            BlockFactory.MarkupTagBlock("</div>", AcceptedCharactersInternal.None),
                            Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                        Factory.Code("}").AsStatement()),
                    Factory.Code(" }").AsStatement()));
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments()
        {
            SingleSpanBlockTest(@"if(foo) {
    // bar } "" baz '
    zoop();
}", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void NestedCodeBlockWithAtDoesntCauseError()
        {
            ParseBlockTest("if (true) { @if(false) { } }",
                           new StatementBlock(
                               Factory.Code("if (true) { ").AsStatement(),
                               new StatementBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("if(false) { }").AsStatement()
                                   ),
                               Factory.Code(" }").AsStatement()));
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideBlockComments()
        {
            SingleSpanBlockTest(
                @"if(foo) {
    /* bar } "" */ ' baz } '
    zoop();
}", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            SingleSpanBlockTest(
                "for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            SingleSpanBlockTest(
                "foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            SingleSpanBlockTest(
                "while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen()
        {
            SingleSpanBlockTest(
                "using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsUsingsNestedWithinOtherBlocks()
        {
            SingleSpanBlockTest(
                "if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            SingleSpanBlockTest(
                "if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockAllowsEmptyBlockStatement()
        {
            SingleSpanBlockTest("if(false) { }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockTerminatesParenBalancingAtEOF()
        {
            ImplicitExpressionTest(
                "Html.En(code()", "Html.En(code()",
                AcceptedCharactersInternal.Any,
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                    new SourceLocation(8, 0, 8),
                    length: 1)));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } ", " else { baz(); }",
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }",
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code);
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
else { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code);
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

            SingleSpanBlockTest(document, BlockKindInternal.Statement, SpanKindInternal.Code);
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
            SingleSpanBlockTest(document, BlockKindInternal.Statement, SpanKindInternal.Code);
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

            SingleSpanBlockTest(document, BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
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
            const string expected = ifStatement + elseIfBranch + elseBranch;

            ParseBlockTest(
                document,
                new StatementBlock(Factory.Code(expected).AsStatement().Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlockStopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";

            SingleSpanBlockTest(document, BlockKindInternal.Statement, SpanKindInternal.Code);
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

            SingleSpanBlockTest(document, BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlock()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while(foo != bar);",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar)", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            SingleSpanBlockTest("do { var foo = bar; } while", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while;",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            SingleSpanBlockTest("do { var foo = bar; } narf;", "do { var foo = bar; }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } /* Foo */ /* Bar */ while(true);",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(@"do { var foo = bar; }
// Foo
// Bar
while(true);", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest(
                "do { var foo = bar; } ", " while(true);",
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesMarkupInDoWhileBlock()
        {
            ParseBlockTest("@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("do { var foo = bar;").AsStatement(),
                               new MarkupBlock(
                                    Factory.Markup(" "),
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Foo"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)
                                   ),
                               Factory.Code("foo++; } while (foo<bar>);").AsStatement().Accepts(AcceptedCharactersInternal.None)
                               ));
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
}", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsLockKeyword()
        {
            SingleSpanBlockTest(
                "lock(foo) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceImportMissingSemicolon()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz",
                " Foo.Bar.Baz",
                acceptedCharacters: AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace,
                location: new SourceLocation(17, 0, 17));
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceAliasMissingSemicolon()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz = FooBarBaz",
                " Foo.Bar.Baz = FooBarBaz",
                acceptedCharacters: AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace,
                location: new SourceLocation(29, 0, 29));
        }

        [Fact]
        public void ParseBlockParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz;",
                " Foo.Bar.Baz",
                AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace);
        }

        [Fact]
        public void ParseBlockDoesntCaptureWhitespaceAfterUsing()
        {
            ParseBlockTest("using Foo   ",
                           new DirectiveBlock(
                               Factory.Code("using Foo")
                                   .AsNamespaceImport(" Foo")
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace)));
        }

        [Fact]
        public void ParseBlockCapturesNewlineAfterUsing()
        {
            ParseBlockTest($"using Foo{Environment.NewLine}",
                           new DirectiveBlock(
                               Factory.Code($"using Foo{Environment.NewLine}")
                                   .AsNamespaceImport(" Foo")
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace)));
        }

        [Fact]
        public void ParseBlockParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest(
                "using FooBarBaz = FooBarBaz;",
                " FooBarBaz = FooBarBaz",
                AcceptedCharactersInternal.NonWhiteSpace | AcceptedCharactersInternal.WhiteSpace);
        }

        [Fact]
        public void ParseBlockTerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            SingleSpanBlockTest("using                    ", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { // foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(SourceLocation.Zero, contentLength: 1), "foreach", "}", "{"));
        }

        [Fact]
        public void ParseBlockTerminatesBlockCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { /* foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                RazorDiagnosticFactory.CreateParsing_BlockCommentNotTerminated(
                    new SourceSpan(new SourceLocation(24, 0, 24), contentLength: 1)),
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(SourceLocation.Zero, contentLength: 1), "foreach", "}", "{"));
        }

        [Fact]
        public void ParseBlockTerminatesSingleSlashAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { / foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(SourceLocation.Zero, contentLength: 1), "foreach", "}", "{"));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ finally { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } ", " finally { biz(); }", acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(
                "try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest(
                "try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }",
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code);
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
finally { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithNoAdditionalClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinTryClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try {",
                markup: " <p>Foo</p> ",
                suffix: "}",
                expectedStart: new SourceLocation(5, 0, 5),
                expectedMarkup: new MarkupBlock(
                    Factory.Markup(" "),
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithOneCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinCatchClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}",
                expectedStart: new SourceLocation(46, 0, 46),
                expectedMarkup: new MarkupBlock(
                    Factory.Markup(" "),
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithMultipleCatchClause()
        {
            SingleSpanBlockTest(
                "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }",
                BlockKindInternal.Statement,
                SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsExceptionLessCatchClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch { var foo = new { } }", BlockKindInternal.Statement, SpanKindInternal.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinAdditionalCatchClauses()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) {",
                markup: " <p>Foo</p> ",
                suffix: "}",
                expectedStart: new SourceLocation(128, 0, 128),
                expectedMarkup: new MarkupBlock(
                    Factory.Markup(" "),
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithFinallyClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } finally { var foo = new { } }", BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinFinallyClause()
        {
            RunSimpleWrappedMarkupTest(
                prefix: "try { var foo = new { } } finally {",
                markup: " <p>Foo</p> ",
                suffix: "}",
                expectedStart: new SourceLocation(35, 0, 35),
                expectedMarkup: new MarkupBlock(
                    Factory.Markup(" "),
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockStopsParsingCatchClausesAfterFinallyBlock()
        {
            var expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " catch(Foo Bar Baz) { }", expectedContent, BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockDoesNotAllowMultipleFinallyBlocks()
        {
            var expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " finally { }", expectedContent, BlockKindInternal.Statement, SpanKindInternal.Code, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockAcceptsTrailingDotIntoImplicitExpressionWhenEmbeddedInCode()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo. }",
                           new StatementBlock(
                               Factory.Code("if(foo) { ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                                   ),
                               Factory.Code(" }").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockParsesExpressionOnSwitchCharacterFollowedByOpenParen()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @(foo + bar) }",
                           new StatementBlock(
                               Factory.Code("if(foo) { ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                                   Factory.Code("foo + bar").AsExpression(),
                                   Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                                   ),
                               Factory.Code(" }").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockParsesExpressionOnSwitchCharacterFollowedByIdentifierStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @foo[4].bar() }",
                           new StatementBlock(
                               Factory.Code("if(foo) { ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo[4].bar()")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                                   ),
                               Factory.Code(" }").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockTreatsDoubleAtSignAsEscapeSequenceIfAtStatementStart()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@class.Foo() }",
                           new StatementBlock(
                               Factory.Code("if(foo) { ").AsStatement(),
                               Factory.Code("@").Hidden(),
                               Factory.Code("@class.Foo() }").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockTreatsAtSignsAfterFirstPairAsPartOfCSharpStatement()
        {
            // Arrange
            ParseBlockTest(@"if(foo) { @@@@class.Foo() }",
                           new StatementBlock(
                               Factory.Code("if(foo) { ").AsStatement(),
                               Factory.Code("@").Hidden(),
                               Factory.Code("@@@class.Foo() }").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockDoesNotParseMarkupStatementOrExpressionOnSwitchCharacterNotFollowedByOpenAngleOrColon()
        {
            // Arrange
            ParseBlockTest("if(foo) { @\"Foo\".ToString(); }",
                           new StatementBlock(
                               Factory.Code("if(foo) { @\"Foo\".ToString(); }").AsStatement()));
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
                         + "        }",
                new StatementBlock(
                    Factory.Code("foreach(var c in db.Categories) {" + Environment.NewLine).AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("            "),
                        BlockFactory.MarkupTagBlock("<div>", AcceptedCharactersInternal.None),
                        Factory.Markup(Environment.NewLine + "                "),
                        BlockFactory.MarkupTagBlock("<h1>", AcceptedCharactersInternal.None),
                        Factory.EmptyHtml(),
                        new ExpressionBlock(
                            Factory.CodeTransition(),
                            Factory.Code("c.Name")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        BlockFactory.MarkupTagBlock("</h1>", AcceptedCharactersInternal.None),
                        Factory.Markup(Environment.NewLine + "                "),
                        BlockFactory.MarkupTagBlock("<ul>", AcceptedCharactersInternal.None),
                        Factory.Markup(Environment.NewLine),
                        new StatementBlock(
                            Factory.Code(@"                    ").AsStatement(),
                            Factory.CodeTransition(),
                            Factory.Code("foreach(var p in c.Products) {" + Environment.NewLine).AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("                        "),
                                BlockFactory.MarkupTagBlock("<li>", AcceptedCharactersInternal.None),
                                new MarkupTagBlock(
                                    Factory.Markup("<a"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            "href",
                                            new LocationTagged<string>(" href=\"", 183 + Environment.NewLine.Length * 5, 5, 30),
                                            new LocationTagged<string>("\"", 246 + Environment.NewLine.Length * 5, 5, 93)),
                                        Factory.Markup(" href=\"").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(string.Empty, 190 + Environment.NewLine.Length * 5, 5, 37), 190 + Environment.NewLine.Length * 5, 5, 37),
                                            new ExpressionBlock(
                                                Factory.CodeTransition(),
                                                Factory.Code("Html.ActionUrl(\"Products\", \"Detail\", new { id = p.Id })")
                                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                        Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    Factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                Factory.EmptyHtml(),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("p.Name")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                                BlockFactory.MarkupTagBlock("</a>", AcceptedCharactersInternal.None),
                                BlockFactory.MarkupTagBlock("</li>", AcceptedCharactersInternal.None),
                                Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None)),
                            Factory.Code("                    }" + Environment.NewLine).AsStatement().Accepts(AcceptedCharactersInternal.None)),
                        Factory.Markup("                "),
                        BlockFactory.MarkupTagBlock("</ul>", AcceptedCharactersInternal.None),
                        Factory.Markup(Environment.NewLine + "            "),
                        BlockFactory.MarkupTagBlock("</div>", AcceptedCharactersInternal.None),
                        Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None)),
                    Factory.Code("        }").AsStatement().Accepts(AcceptedCharactersInternal.None)));
        }

        public static TheoryData BlockWithEscapedTransitionData
        {
            get
            {
                var factory = new SpanFactory();
                var datetimeBlock = new ExpressionBlock(
                    factory.CodeTransition(),
                    factory.Code("DateTime.Now")
                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace));

                return new TheoryData<string, Block>
                {
                    {
                        // Double transition in attribute value
                        "{<span foo='@@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 14, 0, 14)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition at the end of attribute value
                        "{<span foo='abc@@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 17, 0, 17)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("abc", 12, 0, 12))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("@", 15, 0, 15))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition at the beginning attribute value
                        "{<span foo='@@def' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 17, 0, 17)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), new LocationTagged<string>("def", 14, 0, 14))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition in between attribute value
                        "{<span foo='abc @@ def' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 22, 0, 22)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("abc", 12, 0, 12))),
                                        new MarkupBlock(
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 15, 0, 15), new LocationTagged<string>("@", 16, 0, 16))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup(" def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 18, 0, 18), new LocationTagged<string>("def", 19, 0, 19))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition with expression block
                        "{<span foo='@@@DateTime.Now' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 27, 0, 27)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), 14, 0, 14),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            datetimeBlock),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        "{<span foo='@DateTime.Now @@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 28, 0, 28)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), 12, 0, 12),
                                            datetimeBlock),
                                        new MarkupBlock(
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 25, 0, 25), new LocationTagged<string>("@", 26, 0, 26))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        "{<span foo='@DateTime.Now@@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 27, 0, 27)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), 12, 0, 12),
                                            datetimeBlock),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 25, 0, 25), new LocationTagged<string>("@", 25, 0, 25))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        "{<span foo='@(2+3)@@@DateTime.Now' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 33, 0, 33)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), 12, 0, 12),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None),
                                                factory.Code("2+3").AsExpression(),
                                                factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 18, 0, 18), new LocationTagged<string>("@", 18, 0, 18))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 20, 0, 20), 20, 0, 20),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            datetimeBlock),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        "{<span foo='@@@(2+3)' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 20, 0, 20)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), 14, 0, 14),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None),
                                                factory.Code("2+3").AsExpression(),
                                                factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition with email in attribute value
                        "{<span foo='abc@def.com @@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 26, 0, 26)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        factory.Markup("abc@def.com").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("abc@def.com", 12, 0, 12))),
                                        new MarkupBlock(
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 23, 0, 23), new LocationTagged<string>("@", 24, 0, 24))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        "{<span foo='abc@@def.com @@' />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 27, 0, 27)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("abc", 12, 0, 12))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("@", 15, 0, 15))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("def.com").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 17, 0, 17), new LocationTagged<string>("def.com", 17, 0, 17))),
                                        new MarkupBlock(
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 24, 0, 24), new LocationTagged<string>("@", 25, 0, 25))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                    {
                        // Double transition in complex regex in attribute value
                        @"{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}",
                        CreateStatementBlock(
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo=\"", 6, 0, 6), new LocationTagged<string>("\"", 112, 0, 112)),
                                        factory.Markup(" foo=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup(@"/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>(@"/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+", 12, 0, 12))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 44, 0, 44), new LocationTagged<string>("@", 44, 0, 44))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 46, 0, 46), new LocationTagged<string>(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i", 46, 0, 46))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BlockWithEscapedTransitionData))]
        public void ParseBlock_WithDoubleTransition_DoesNotThrow(string input, object expected)
        {
            FixupSpans = true;

            // Act & Assert
            ParseBlockTest(input, (Block)expected);
        }

        [Fact]
        public void ParseBlock_WithDoubleTransition_EndOfFile_Throws()
        {
            // Arrange
            var expected = new StatementBlock(
                Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>(string.Empty, 14, 0, 14)),
                        Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                        new MarkupBlock(
                            Factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharactersInternal.None),
                            Factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)))),
                Factory.EmptyHtml()));
            var expectedErrors = new RazorDiagnostic[]
            {
                RazorDiagnostic.Create(new RazorError(
                    @"End of file or an unexpected character was reached before the ""span"" tag could be parsed.  Elements inside markup blocks must be complete. They must either be self-closing (""<br />"") or have matching end tags (""<p>Hello</p>"").  If you intended to display a ""<"" character, use the ""&lt;"" HTML entity.",
                    new SourceLocation(2, 0, 2),
                    length: 4)),
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_Code, "}", "{"),
            };

            // Act & Assert
            ParseBlockTest("{<span foo='@@", expected, expectedErrors);
        }

        [Fact]
        public void ParseBlock_WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            // Arrange
            var expected = new StatementBlock(
                Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>("'", 15, 0, 15)),
                        Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), 12, 0, 12),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(" ", 13, 0, 13), 13, 0, 13),
                            Factory.Markup(" ").With(SpanChunkGenerator.Null),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                        Factory.Markup("'").With(SpanChunkGenerator.Null)),
                    Factory.Markup(" />").Accepts(AcceptedCharactersInternal.None))),
                Factory.EmptyCSharp().AsStatement(),
                Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None));
            var expectedErrors = new RazorDiagnostic[]
            {
                RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                    new SourceSpan(new SourceLocation(13, 0, 13), contentLength: 1)),
                RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                    new SourceSpan(new SourceLocation(15, 0, 15), contentLength: 5),
                    "' />}"),
            };

            // Act & Assert
            ParseBlockTest("{<span foo='@ @' />}", expected, expectedErrors);
        }

        private void RunRazorCommentBetweenClausesTest(string preComment, string postComment, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            ParseBlockTest(preComment + "@* Foo *@ @* Bar *@" + postComment,
                           new StatementBlock(
                               Factory.Code(preComment).AsStatement(),
                               new CommentBlock(
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                                   Factory.Comment(" Foo ", CSharpSymbolType.RazorComment),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   ),
                               Factory.Code(" ").AsStatement(),
                               new CommentBlock(
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                                   Factory.Comment(" Bar ", CSharpSymbolType.RazorComment),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   ),
                               Factory.Code(postComment).AsStatement().Accepts(acceptedCharacters)));
        }

        private void RunSimpleWrappedMarkupTest(string prefix, string markup, string suffix, MarkupBlock expectedMarkup, SourceLocation expectedStart, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            var expected = new StatementBlock(
                    Factory.Code(prefix).AsStatement(),
                    expectedMarkup,
                    Factory.Code(suffix).AsStatement().Accepts(acceptedCharacters));

            // Since we're building the 'expected' input out of order we need to do some trickery
            // to get the locations right.
            SpancestryCorrector.Correct(expected);
            expected.FindFirstDescendentSpan().ChangeStart(SourceLocation.Zero);

            // We make the caller pass a start location so we can verify that nothing has gone awry.
            Assert.Equal(expectedStart, expectedMarkup.Start);

            ParseBlockTest(prefix + markup + suffix, expected);
        }

        private void NamespaceImportTest(string content, string expectedNS, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.None, string errorMessage = null, SourceLocation? location = null)
        {
            var errors = new RazorDiagnostic[0];
            if (!string.IsNullOrEmpty(errorMessage) && location.HasValue)
            {
                errors = new RazorDiagnostic[]
                {
                    RazorDiagnostic.Create(new RazorError(errorMessage, location.Value, length: 1))
                };
            }
            ParseBlockTest(content,
                           new DirectiveBlock(
                               Factory.Code(content)
                                   .AsNamespaceImport(expectedNS)
                                   .Accepts(acceptedCharacters)),
                           errors);
        }

        private static StatementBlock CreateStatementBlock(MarkupBlock block)
        {
            var factory = new SpanFactory();
            return new StatementBlock(
                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                block,
                factory.EmptyCSharp().AsStatement(),
                factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None));
        }
    }
}
