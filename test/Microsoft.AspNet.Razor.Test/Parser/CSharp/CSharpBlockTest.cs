// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpBlockTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockMethodThrowsArgNullExceptionOnNullContext()
        {
            // Arrange
            CSharpCodeParser parser = new CSharpCodeParser();

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => parser.ParseBlock(), RazorResources.Parser_Context_Not_Set);
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideSingleLineComments()
        {
            SingleSpanBlockTest(@"if(foo) {
    // bar } "" baz '
    zoop();
}", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void NestedCodeBlockWithAtCausesError()
        {
            ParseBlockTest("if (true) { @if(false) { } }",
                           new StatementBlock(
                               Factory.Code("if (true) { ").AsStatement(),
                               new StatementBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("if(false) { }").AsStatement()
                                   ),
                               Factory.Code(" }").AsStatement()),
                           new RazorError(
                               RazorResources.ParseError_Unexpected_Keyword_After_At("if"),
                               new SourceLocation(13, 0, 13)));
        }

        [Fact]
        public void BalancingBracketsIgnoresStringLiteralCharactersAndBracketsInsideBlockComments()
        {
            SingleSpanBlockTest(
                @"if(foo) {
    /* bar } "" */ ' baz } '
    zoop();
}", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForKeyword()
        {
            SingleSpanBlockTest("for(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsForeachKeyword()
        {
            SingleSpanBlockTest("foreach(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsWhileKeyword()
        {
            SingleSpanBlockTest("while(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsUsingKeywordFollowedByParen()
        {
            SingleSpanBlockTest("using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsUsingsNestedWithinOtherBlocks()
        {
            SingleSpanBlockTest("if(foo) { using(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); } }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsIfKeywordWithNoElseBranches()
        {
            SingleSpanBlockTest("if(int i = 0; i < 10; new Foo { Bar = \"baz\" }) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockAllowsEmptyBlockStatement()
        {
            SingleSpanBlockTest("if(false) { }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockTerminatesParenBalancingAtEOF()
        {
            ImplicitExpressionTest("Html.En(code()", "Html.En(code()",
                                   AcceptedCharacters.Any,
                                   new RazorError(
                                       RazorResources.ParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                                       new SourceLocation(8, 0, 8)));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseClause()
        {
            SingleSpanBlockTest("if(foo) { bar(); } /* Foo */ /* Bar */ else { baz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest("if(foo) { bar(); } ", " else { baz(); }", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest("if(foo) { bar(); } else if(bar) { baz(); } /* Foo */ /* Bar */ else { biz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenElseIfAndElseClause()
        {
            RunRazorCommentBetweenClausesTest("if(foo) { bar(); } else if(bar) { baz(); } ", " else { baz(); }", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest("if(foo) { bar(); } /* Foo */ /* Bar */ else if(bar) { baz(); }", BlockType.Statement, SpanKind.Code);
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
else { baz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenElseIfAndElseClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); } else if(bar) { baz(); }
// Foo
// Bar
else { biz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenIfAndElseIfClause()
        {
            SingleSpanBlockTest(@"if(foo) { bar(); }
// Foo
// Bar
else if(bar) { baz(); }", BlockType.Statement, SpanKind.Code);
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

            SingleSpanBlockTest(document, BlockType.Statement, SpanKind.Code);
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
            SingleSpanBlockTest(document, BlockType.Statement, SpanKind.Code);
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

            SingleSpanBlockTest(document, BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
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

            ParseBlockTest(document, new StatementBlock(Factory.Code(expected).AsStatement().Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockStopsParsingIfIfStatementNotFollowedByElse()
        {
            const string document = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar""); 
}";

            SingleSpanBlockTest(document, BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockAcceptsElseIfWithNoCondition()
        {
            // We don't want to be a full C# parser - If the else if is missing it's condition, the C# compiler can handle that, we have all the info we need to keep parsing
            const string ifBranch = @"if(int i = 0; i < 10; new Foo { Bar = ""baz"" }) {
    Debug.WriteLine(@""foo } bar""); 
}";
            const string elseIfBranch = @" else if { foo(); }";
            const string document = ifBranch + elseIfBranch;

            SingleSpanBlockTest(document, BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlock()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar);", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while(foo != bar)", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileCondition()
        {
            SingleSpanBlockTest("do { var foo = bar; } while", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileConditionWithSemicolon()
        {
            SingleSpanBlockTest("do { var foo = bar; } while;", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesDoWhileBlockMissingWhileClauseEntirely()
        {
            SingleSpanBlockTest("do { var foo = bar; } narf;", "do { var foo = bar; }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest("do { var foo = bar; } /* Foo */ /* Bar */ while(true);", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenDoAndWhileClause()
        {
            SingleSpanBlockTest(@"do { var foo = bar; } 
// Foo
// Bar
while(true);", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenDoAndWhileClause()
        {
            RunRazorCommentBetweenClausesTest("do { var foo = bar; } ", " while(true);", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCorrectlyParsesMarkupInDoWhileBlock()
        {
            ParseBlockTest("@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("do { var foo = bar;").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Foo</p> ").Accepts(AcceptedCharacters.None)
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
}", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSkipsParenthesisedExpressionAndThenBalancesBracesIfFirstIdentifierIsLockKeyword()
        {
            SingleSpanBlockTest("lock(foo) { Debug.WriteLine(@\"foo } bar\"); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceImportMissingSemicolon()
        {
            NamespaceImportTest("using Foo.Bar.Baz", " Foo.Bar.Baz", acceptedCharacters: AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace, location: new SourceLocation(17, 0, 17));
        }

        [Fact]
        public void ParseBlockHasErrorsIfNamespaceAliasMissingSemicolon()
        {
            NamespaceImportTest("using Foo.Bar.Baz = FooBarBaz", " Foo.Bar.Baz = FooBarBaz", acceptedCharacters: AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace, location: new SourceLocation(29, 0, 29));
        }

        [Fact]
        public void ParseBlockParsesNamespaceImportWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest("using Foo.Bar.Baz;", " Foo.Bar.Baz", AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace);
        }

        [Fact]
        public void ParseBlockDoesntCaptureWhitespaceAfterUsing()
        {
            ParseBlockTest("using Foo   ",
                           new DirectiveBlock(
                               Factory.Code("using Foo")
                                   .AsNamespaceImport(" Foo", CSharpCodeParser.UsingKeywordLength)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void ParseBlockParsesNamespaceAliasWithSemicolonForUsingKeywordIfIsInValidFormat()
        {
            NamespaceImportTest("using FooBarBaz = FooBarBaz;", " FooBarBaz = FooBarBaz", AcceptedCharacters.NonWhiteSpace | AcceptedCharacters.WhiteSpace);
        }

        [Fact]
        public void ParseBlockTerminatesUsingKeywordAtEOFAndOutputsFileCodeBlock()
        {
            SingleSpanBlockTest("using                    ", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { // foo bar baz";
            SingleSpanBlockTest(document, document, BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockTerminatesBlockCommentAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { /* foo bar baz";
            SingleSpanBlockTest(document, document, BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_BlockComment_Not_Terminated, 24, 0, 24),
                                new RazorError(RazorResources.ParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockTerminatesSingleSlashAtEndOfFile()
        {
            const string document = "foreach(var f in Foo) { / foo bar baz";
            SingleSpanBlockTest(document, document, BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_Expected_EndOfBlock_Before_EOF("foreach", '}', '{'), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ finally { baz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenTryAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } ", " finally { biz(); }", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest("try { bar(); } catch(bar) { baz(); } /* Foo */ /* Bar */ finally { biz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsRazorCommentBetweenCatchAndFinallyClause()
        {
            RunRazorCommentBetweenClausesTest("try { bar(); } catch(bar) { baz(); } ", " finally { biz(); }", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsBlockCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest("try { bar(); } /* Foo */ /* Bar */ catch(bar) { baz(); }", BlockType.Statement, SpanKind.Code);
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
finally { baz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenCatchAndFinallyClause()
        {
            SingleSpanBlockTest(@"try { bar(); } catch(bar) { baz(); }
// Foo
// Bar
finally { biz(); }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsLineCommentBetweenTryAndCatchClause()
        {
            SingleSpanBlockTest(@"try { bar(); }
// Foo
// Bar
catch(bar) { baz(); }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithNoAdditionalClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinTryClause()
        {
            RunSimpleWrappedMarkupTest("try {", " <p>Foo</p> ", "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithOneCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinCatchClause()
        {
            RunSimpleWrappedMarkupTest("try { var foo = new { } } catch(Foo Bar Baz) {", " <p>Foo</p> ", "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithMultipleCatchClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsExceptionLessCatchClauses()
        {
            SingleSpanBlockTest("try { var foo = new { } } catch { var foo = new { } }", BlockType.Statement, SpanKind.Code);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinAdditionalCatchClauses()
        {
            RunSimpleWrappedMarkupTest("try { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) { var foo = new { } } catch(Foo Bar Baz) {", " <p>Foo</p> ", "}");
        }

        [Fact]
        public void ParseBlockSupportsTryStatementWithFinallyClause()
        {
            SingleSpanBlockTest("try { var foo = new { } } finally { var foo = new { } }", BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsMarkupWithinFinallyClause()
        {
            RunSimpleWrappedMarkupTest("try { var foo = new { } } finally {", " <p>Foo</p> ", "}", acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockStopsParsingCatchClausesAfterFinallyBlock()
        {
            string expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " catch(Foo Bar Baz) { }", expectedContent, BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockDoesNotAllowMultipleFinallyBlocks()
        {
            string expectedContent = "try { var foo = new { } } finally { var foo = new { } }";
            SingleSpanBlockTest(expectedContent + " finally { }", expectedContent, BlockType.Statement, SpanKind.Code, acceptedCharacters: AcceptedCharacters.None);
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
                    Factory.Code("foreach(var c in db.Categories) {\r\n").AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("            <div>\r\n                <h1>"),
                        new ExpressionBlock(
                            Factory.CodeTransition(),
                            Factory.Code("c.Name")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        Factory.Markup("</h1>\r\n                <ul>\r\n"),
                        new StatementBlock(
                            Factory.Code(@"                    ").AsStatement(),
                            Factory.CodeTransition(),
                            Factory.Code("foreach(var p in c.Products) {\r\n").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("                        <li><a"),
                                new MarkupBlock(new AttributeBlockCodeGenerator("href", new LocationTagged<string>(" href=\"", 193, 5, 30), new LocationTagged<string>("\"", 256, 5, 93)),
                                    Factory.Markup(" href=\"").With(SpanCodeGenerator.Null),
                                    new MarkupBlock(new DynamicAttributeBlockCodeGenerator(new LocationTagged<string>(String.Empty, 200, 5, 37), 200, 5, 37),
                                        new ExpressionBlock(
                                            Factory.CodeTransition(),
                                            Factory.Code("Html.ActionUrl(\"Products\", \"Detail\", new { id = p.Id })")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                   .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                Factory.Markup(">"),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("p.Name")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                Factory.Markup("</a></li>\r\n").Accepts(AcceptedCharacters.None)),
                            Factory.Code("                    }\r\n").AsStatement().Accepts(AcceptedCharacters.None)),
                        Factory.Markup("                </ul>\r\n            </div>\r\n")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("        }").AsStatement().Accepts(AcceptedCharacters.None)));
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

        private void RunSimpleWrappedMarkupTest(string prefix, string markup, string suffix, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            ParseBlockTest(prefix + markup + suffix,
                           new StatementBlock(
                               Factory.Code(prefix).AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(markup).Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code(suffix).AsStatement().Accepts(acceptedCharacters)
                               ));
        }

        private void NamespaceImportTest(string content, string expectedNS, AcceptedCharacters acceptedCharacters = AcceptedCharacters.None, string errorMessage = null, SourceLocation? location = null)
        {
            var errors = new RazorError[0];
            if (!String.IsNullOrEmpty(errorMessage) && location.HasValue)
            {
                errors = new RazorError[]
                {
                    new RazorError(errorMessage, location.Value)
                };
            }
            ParseBlockTest(content,
                           new DirectiveBlock(
                               Factory.Code(content)
                                   .AsNamespaceImport(expectedNS, CSharpCodeParser.UsingKeywordLength)
                                   .Accepts(acceptedCharacters)),
                           errors);
        }
    }
}
