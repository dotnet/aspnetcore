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
                    Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                    Factory
                        .Code(" if (true) { var val = @x; if (val != 3) { } } ")
                        .AsStatement()
                        .Accepts(AcceptedCharacters.Any)
                        .AutoCompleteWith(autoCompleteString: null, atEndOfSpan: false),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
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
                            BlockFactory.MarkupTagBlock("<div>", AcceptedCharacters.None),
                            Factory.EmptyHtml(),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("something")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                                    .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("."),
                            BlockFactory.MarkupTagBlock("</div>", AcceptedCharacters.None),
                            Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                        Factory.Code("}").AsStatement()),
                    Factory.Code(" }").AsStatement()));
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments()
        {
            SingleSpanBlockTest(@"if(foo) {
    // bar } "" baz '
    zoop();
}", BlockKind.Statement, SpanKind.Code);
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
}", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            SingleSpanBlockTest(
                "for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            SingleSpanBlockTest(
                "foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            SingleSpanBlockTest(
                "while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen()
        {
            SingleSpanBlockTest(
                "using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsUsingsNestedWithinOtherBlocks()
        {
            SingleSpanBlockTest(
                "if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }",
                BlockKind.Statement,
                SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            SingleSpanBlockTest(
                "if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code);
        }

        [Fact]
        public void ParseBlockAllowsEmptyBlockStatement()
        {
            SingleSpanBlockTest("if(false) { }", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockTerminatesParenBalancingAtEOF()
        {
            ImplicitExpressionTest(
                "Html.En(code()", "Html.En(code()",
                AcceptedCharacters.Any,
                new RazorError(
                    LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                    new SourceLocation(8, 0, 8),
                    length: 1));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } ", " else { baz(); }",
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest(
                "if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }",
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(
                "if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }",
                BlockKind.Statement,
                SpanKind.Code);
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
else { baz(); }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }", BlockKind.Statement, SpanKind.Code);
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

            SingleSpanBlockTest(document, BlockKind.Statement, SpanKind.Code);
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
            SingleSpanBlockTest(document, BlockKind.Statement, SpanKind.Code);
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

            SingleSpanBlockTest(document, BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
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
                new StatementBlock(Factory.Code(expected).AsStatement().Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockStopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar"");
}";

            SingleSpanBlockTest(document, BlockKind.Statement, SpanKind.Code);
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

            SingleSpanBlockTest(document, BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlock()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while(foo != bar);",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar)", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            SingleSpanBlockTest("do { var foo = bar; } while", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } while;",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            SingleSpanBlockTest("do { var foo = bar; } narf;", "do { var foo = bar; }", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(
                "do { var foo = bar; } /* Foo */ /* Bar */ while(true);",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(@"do { var foo = bar; }
// Foo
// Bar
while(true);", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest(
                "do { var foo = bar; } ", " while(true);",
                acceptedCharacters: AcceptedCharacters.None);
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
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharacters.None),
                                    Factory.Markup("Foo"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("foo++; } while (foo<bar>);").AsStatement().Accepts(AcceptedCharacters.None)
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
}", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsLockKeyword()
        {
            SingleSpanBlockTest(
                "lock(foo) { Debug.WriteLine(@\"foo } bar\"); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceImportMissingSemicolon()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz",
                " Foo.Bar.Baz",
                acceptedCharacters: AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace,
                location: new SourceLocation(17, 0, 17));
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceAliasMissingSemicolon()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz = FooBarBaz",
                " Foo.Bar.Baz = FooBarBaz",
                acceptedCharacters: AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace,
                location: new SourceLocation(29, 0, 29));
        }

        [Fact]
        public void ParseBlockParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest(
                "using Foo.Bar.Baz;",
                " Foo.Bar.Baz",
                AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace);
        }

        [Fact]
        public void ParseBlockDoesntCaptureWhitespaceAfterUsing()
        {
            ParseBlockTest("using Foo   ",
                           new DirectiveBlock(
                               Factory.Code("using Foo")
                                   .AsNamespaceImport(" Foo")
                                   .Accepts(AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void ParseBlockCapturesNewlineAfterUsing()
        {
            ParseBlockTest($"using Foo{Environment.NewLine}",
                           new DirectiveBlock(
                               Factory.Code($"using Foo{Environment.NewLine}")
                                   .AsNamespaceImport(" Foo")
                                   .Accepts(AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void ParseBlockParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest(
                "using FooBarBaz = FooBarBaz;",
                " FooBarBaz = FooBarBaz",
                AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace);
        }

        [Fact]
        public void ParseBlockTerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            SingleSpanBlockTest("using                    ", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { // foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKind.Statement,
                SpanKind.Code,
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'),
                    SourceLocation.Zero,
                    length: 1));
        }

        [Fact]
        public void ParseBlockTerminatesBlockCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { /* foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKind.Statement,
                SpanKind.Code,
                new RazorError(
                    LegacyResources.ParseError_BlockComment_Not_Terminated,
                    new SourceLocation(24, 0, 24),
                    length: 1),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'),
                    SourceLocation.Zero,
                    length: 1));
        }

        [Fact]
        public void ParseBlockTerminatesSingleSlashAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { / foo bar baz";
            SingleSpanBlockTest(
                document,
                document,
                BlockKind.Statement,
                SpanKind.Code,
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'),
                    SourceLocation.Zero,
                    length: 1));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ finally { baz(); }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } ", " finally { biz(); }", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(
                "try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }",
                BlockKind.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest(
                "try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }",
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }", BlockKind.Statement, SpanKind.Code);
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
finally { baz(); }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }", BlockKind.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithNoAdditionalClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } }", BlockKind.Statement, SpanKind.Code);
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
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharacters.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithOneCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }", BlockKind.Statement, SpanKind.Code);
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
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharacters.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithMultipleCatchClause()
        {
            SingleSpanBlockTest(
                "try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) " +
                "{ var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }",
                BlockKind.Statement,
                SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsExceptionLessCatchClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch { var foo = new { } }", BlockKind.Statement, SpanKind.Code);
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
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharacters.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithFinallyClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } finally { var foo = new { } }", BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
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
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharacters.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockStopsParsingCatchClausesAfterFinallyBlock()
        {
            var expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " catch(Foo Bar Baz) { }", expectedContent, BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockDoesNotAllowMultipleFinallyBlocks()
        {
            var expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " finally { }", expectedContent, BlockKind.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
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
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)
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
                                   Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                                   Factory.Code("foo + bar").AsExpression(),
                                   Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
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
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)
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
                        BlockFactory.MarkupTagBlock("<div>", AcceptedCharacters.None),
                        Factory.Markup(Environment.NewLine + "                "),
                        BlockFactory.MarkupTagBlock("<h1>", AcceptedCharacters.None),
                        Factory.EmptyHtml(),
                        new ExpressionBlock(
                            Factory.CodeTransition(),
                            Factory.Code("c.Name")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        BlockFactory.MarkupTagBlock("</h1>", AcceptedCharacters.None),
                        Factory.Markup(Environment.NewLine + "                "),
                        BlockFactory.MarkupTagBlock("<ul>", AcceptedCharacters.None),
                        Factory.Markup(Environment.NewLine),
                        new StatementBlock(
                            Factory.Code(@"                    ").AsStatement(),
                            Factory.CodeTransition(),
                            Factory.Code("foreach(var p in c.Products) {" + Environment.NewLine).AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("                        "),
                                BlockFactory.MarkupTagBlock("<li>", AcceptedCharacters.None),
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
                                                       .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                        Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    Factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                Factory.EmptyHtml(),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("p.Name")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                BlockFactory.MarkupTagBlock("</a>", AcceptedCharacters.None),
                                BlockFactory.MarkupTagBlock("</li>", AcceptedCharacters.None),
                                Factory.Markup(Environment.NewLine).Accepts(AcceptedCharacters.None)),
                            Factory.Code("                    }" + Environment.NewLine).AsStatement().Accepts(AcceptedCharacters.None)),
                        Factory.Markup("                "),
                        BlockFactory.MarkupTagBlock("</ul>", AcceptedCharacters.None),
                        Factory.Markup(Environment.NewLine + "            "),
                        BlockFactory.MarkupTagBlock("</div>", AcceptedCharacters.None),
                        Factory.Markup(Environment.NewLine).Accepts(AcceptedCharacters.None)),
                    Factory.Code("        }").AsStatement().Accepts(AcceptedCharacters.None)));
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
                        .Accepts(AcceptedCharacters.NonWhiteSpace));

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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("@", 15, 0, 15))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), new LocationTagged<string>("def", 14, 0, 14))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 15, 0, 15), new LocationTagged<string>("@", 16, 0, 16))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup(" def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 18, 0, 18), new LocationTagged<string>("def", 19, 0, 19))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), 14, 0, 14),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            datetimeBlock),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 25, 0, 25), new LocationTagged<string>("@", 26, 0, 26))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 25, 0, 25), new LocationTagged<string>("@", 25, 0, 25))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                                factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None),
                                                factory.Code("2+3").AsExpression(),
                                                factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 18, 0, 18), new LocationTagged<string>("@", 18, 0, 18))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 20, 0, 20), 20, 0, 20),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            datetimeBlock),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), 14, 0, 14),
                                            factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None),
                                                factory.Code("2+3").AsExpression(),
                                                factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 23, 0, 23), new LocationTagged<string>("@", 24, 0, 24))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("@", 15, 0, 15))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("def.com").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 17, 0, 17), new LocationTagged<string>("def.com", 17, 0, 17))),
                                        new MarkupBlock(
                                            factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 24, 0, 24), new LocationTagged<string>("@", 25, 0, 25))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 44, 0, 44), new LocationTagged<string>("@", 44, 0, 44))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 46, 0, 46), new LocationTagged<string>(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i", 46, 0, 46))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />").Accepts(AcceptedCharacters.None))))
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
                Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 6, 0, 6), new LocationTagged<string>(string.Empty, 14, 0, 14)),
                        Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                        new MarkupBlock(
                            Factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 12, 0, 12), new LocationTagged<string>("@", 12, 0, 12))).Accepts(AcceptedCharacters.None),
                            Factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)))),
                Factory.EmptyHtml()));
            var expectedErrors = new RazorError[]
            {
                new RazorError(
                    @"End of file or an unexpected character was reached before the ""span"" tag could be parsed.  Elements inside markup blocks must be complete. They must either be self-closing (""<br />"") or have matching end tags (""<p>Hello</p>"").  If you intended to display a ""<"" character, use the ""&lt;"" HTML entity.",
                    new SourceLocation(2, 0, 2),
                    length: 4),
                new RazorError(
                    @"The code block is missing a closing ""}"" character.  Make sure you have a matching ""}"" character for all the ""{"" characters within this block, and that none of the ""}"" characters are being interpreted as markup.",
                    SourceLocation.Zero,
                    length: 1),
            };

            // Act & Assert
            ParseBlockTest("{<span foo='@@", expected, expectedErrors);
        }

        [Fact]
        public void ParseBlock_WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            // Arrange
            var expected = new StatementBlock(
                Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
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
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace))),
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(" ", 13, 0, 13), 13, 0, 13),
                            Factory.Markup(" ").With(SpanChunkGenerator.Null),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace))),
                        Factory.Markup("'").With(SpanChunkGenerator.Null)),
                    Factory.Markup(" />").Accepts(AcceptedCharacters.None))),
                Factory.EmptyCSharp().AsStatement(),
                Factory.MetaCode("}").Accepts(AcceptedCharacters.None));
            var expectedErrors = new RazorError[]
            {
                new RazorError(
                    @"A space or line break was encountered after the ""@"" character.  Only valid identifiers, keywords, comments, ""("" and ""{"" are valid at the start of a code block and they must occur immediately following ""@"" with no space in between.",
                    new SourceLocation(13, 0, 13),
                    length: 1),
                new RazorError(
                    @"""' />}"" is not valid at the start of a code block.  Only identifiers, keywords, comments, ""("" and ""{"" are valid.",
                    new SourceLocation(15, 0, 15),
                    length: 5),
            };

            // Act & Assert
            ParseBlockTest("{<span foo='@ @' />}", expected, expectedErrors);
        }

        private void RunRazorCommentBetweenClausesTest(string preComment, string postComment, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            ParseBlockTest(preComment + "@* Foo *@ @* Bar *@" + postComment,
                           new StatementBlock(
                               Factory.Code(preComment).AsStatement(),
                               new CommentBlock(
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.Comment(" Foo ", CSharpSymbolType.RazorComment),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   ),
                               Factory.Code(" ").AsStatement(),
                               new CommentBlock(
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.Comment(" Bar ", CSharpSymbolType.RazorComment),
                                   Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   ),
                               Factory.Code(postComment).AsStatement().Accepts(acceptedCharacters)));
        }

        private void RunSimpleWrappedMarkupTest(string prefix, string markup, string suffix, MarkupBlock expectedMarkup, SourceLocation expectedStart, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
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

        private void NamespaceImportTest(string content, string expectedNS, AcceptedCharacters acceptedCharacters = AcceptedCharacters.None, string errorMessage = null, SourceLocation? location = null)
        {
            var errors = new RazorError[0];
            if (!string.IsNullOrEmpty(errorMessage) && location.HasValue)
            {
                errors = new RazorError[]
                {
                    new RazorError(errorMessage, location.Value, length: 1)
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
                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                block,
                factory.EmptyCSharp().AsStatement(),
                factory.MetaCode("}").Accepts(AcceptedCharacters.None));
        }
    }
}
