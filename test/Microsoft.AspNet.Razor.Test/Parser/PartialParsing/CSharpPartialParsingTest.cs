// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.PartialParsing
{
    public class CSharpPartialParsingTest : PartialParsingTestBase<CSharpRazorCodeLanguage>
    {
        [Fact]
        public void AwaitPeriodInsertionAcceptedProvisionally()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @await Html. baz");
            var old = new StringTextBuffer("foo @await Html baz");

            // Act and Assert
            RunPartialParseTest(new TextChange(15, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("await Html.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.WhiteSpace | AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")), additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertionsInStatementBlock()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateTime..Now" + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @DateTime.Now" + Environment.NewLine
                                                + "}");

            // Act and Assert
            RunPartialParseTest(new TextChange(17, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime..Now")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertions()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @DateTime..Now baz");
            var old = new StringTextBuffer("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TextChange(13, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime..Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")), additionalFlags: PartialParseResult.Provisional);
        }


        [Fact]
        public void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlockAfterIdentifiers()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateTime." + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @DateTime" + Environment.NewLine
                                                + "}");

            var textChange = new TextChange(15 + Environment.NewLine.Length, 0, old, 1, changed);
            using (var manager = CreateParserManager())
            {
                Action<TextChange, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(textChange);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);
                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(textChange.OldBuffer);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted, "DateTime.");

                old = changed;
                changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateTime.." + Environment.NewLine
                                                    + "}");
                textChange = new TextChange(16 + Environment.NewLine.Length, 0, old, 1, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted, "DateTime..");

                old = changed;
                changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateTime.Now." + Environment.NewLine
                                                    + "}");
                textChange = new TextChange(16 + Environment.NewLine.Length, 0, old, 3, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted, "DateTime.Now.");
            }
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlock()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateT." + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @DateT" + Environment.NewLine
                                                + "}");

            var textChange = new TextChange(12 + Environment.NewLine.Length, 0, old, 1, changed);
            using (var manager = CreateParserManager())
            {
                Action<TextChange, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(textChange);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);
                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(textChange.OldBuffer);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted, "DateT.");

                old = changed;
                changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @DateTime." + Environment.NewLine
                                                    + "}");
                textChange = new TextChange(12 + Environment.NewLine.Length, 0, old, 3, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted, "DateTime.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertions()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @DateT. baz");
            var old = new StringTextBuffer("foo @DateT baz");
            var textChange = new TextChange(10, 0, old, 1, changed);
            using (var manager = CreateParserManager())
            {
                Action<TextChange, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(textChange);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(textChange.OldBuffer);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateT.");

                old = changed;
                changed = new StringTextBuffer("foo @DateTime. baz");
                textChange = new TextChange(10, 0, old, 3, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertionsAfterIdentifiers()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @DateTime. baz");
            var old = new StringTextBuffer("foo @DateTime baz");
            var textChange = new TextChange(13, 0, old, 1, changed);
            using (var manager = CreateParserManager())
            {
                Action<TextChange, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(textChange);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(textChange.OldBuffer);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.");

                old = changed;
                changed = new StringTextBuffer("foo @DateTime.. baz");
                textChange = new TextChange(14, 0, old, 1, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime..");

                old = changed;
                changed = new StringTextBuffer("foo @DateTime.Now. baz");
                textChange = new TextChange(14, 0, old, 3, changed);

                applyAndVerifyPartialChange(textChange, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.Now.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDeleteOfIdentifierPartsIfDotRemains()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @User. baz");
            var old = new StringTextBuffer("foo @User.Name baz");
            RunPartialParseTest(new TextChange(10, 4, old, 0, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsDeleteOfIdentifierPartsIfSomeOfIdentifierRemains()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @Us baz");
            var old = new StringTextBuffer("foo @User baz");
            RunPartialParseTest(new TextChange(7, 2, old, 0, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("Us").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsMultipleInsertionIfItCausesIdentifierExpansionAndTrailingDot()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @User. baz");
            var old = new StringTextBuffer("foo @U baz");
            RunPartialParseTest(new TextChange(6, 0, old, 4, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsMultipleInsertionIfItOnlyCausesIdentifierExpansion()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @barbiz baz");
            var old = new StringTextBuffer("foo @bar baz");
            RunPartialParseTest(new TextChange(8, 0, old, 3, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("barbiz").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierExpansionAtEndOfNonWhitespaceCharacters()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @food" + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TextChange(10 + Environment.NewLine.Length, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("food")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierAfterDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @foo.d" + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @foo." + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TextChange(11 + Environment.NewLine.Length, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.d")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{" + Environment.NewLine
                                                    + "    @foo." + Environment.NewLine
                                                    + "}");
            var old = new StringTextBuffer("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TextChange(10 + Environment.NewLine.Length, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(@"foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionRejectsChangeWhichWouldHaveBeenAcceptedIfLastChangeWasProvisionallyAcceptedOnDifferentSpan()
        {
            var factory = SpanFactory.CreateCsHtml();

            // Arrange
            var dotTyped = new TextChange(8, 0, new StringTextBuffer("foo @foo @bar"), 1, new StringTextBuffer("foo @foo. @bar"));
            var charTyped = new TextChange(14, 0, new StringTextBuffer("foo @foo. @bar"), 1, new StringTextBuffer("foo @foo. @barb"));
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(dotTyped.OldBuffer);

                // Apply the dot change
                Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

                // Act (apply the identifier start char change)
                var result = manager.CheckForStructureChangesAndWait(charTyped);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(". "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("barb")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.EmptyHtml()));
            }
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierTypedAfterDotIfLastChangeWasProvisionalAcceptanceOfDot()
        {
            var factory = SpanFactory.CreateCsHtml();

            // Arrange
            var dotTyped = new TextChange(8, 0, new StringTextBuffer("foo @foo bar"), 1, new StringTextBuffer("foo @foo. bar"));
            var charTyped = new TextChange(9, 0, new StringTextBuffer("foo @foo. bar"), 1, new StringTextBuffer("foo @foo.b bar"));
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(dotTyped.OldBuffer);

                // Apply the dot change
                Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

                // Act (apply the identifier start char change)
                var result = manager.CheckForStructureChangesAndWait(charTyped);

                // Assert
                Assert.Equal(PartialParseResult.Accepted, result);
                Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" bar")));
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotAfterIdentifierInMarkup()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @foo. bar");
            var old = new StringTextBuffer("foo @foo bar");
            RunPartialParseTest(new TextChange(8, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo.")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" bar")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierCharactersIfEndOfSpanIsIdentifier()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("foo @foob bar");
            var old = new StringTextBuffer("foo @foo bar");
            RunPartialParseTest(new TextChange(8, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foob")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" bar")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierStartCharactersIfEndOfSpanIsDot()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{@foo.b}");
            var old = new StringTextBuffer("@{@foo.}");
            RunPartialParseTest(new TextChange(7, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotIfTrailingDotsAreAllowed()
        {
            var factory = SpanFactory.CreateCsHtml();
            var changed = new StringTextBuffer("@{@foo.}");
            var old = new StringTextBuffer("@{@foo}");
            RunPartialParseTest(new TextChange(6, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfIfKeywordTyped()
        {
            RunTypeKeywordTest("if");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfDoKeywordTyped()
        {
            RunTypeKeywordTest("do");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfTryKeywordTyped()
        {
            RunTypeKeywordTest("try");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfForKeywordTyped()
        {
            RunTypeKeywordTest("for");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfForEachKeywordTyped()
        {
            RunTypeKeywordTest("foreach");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfWhileKeywordTyped()
        {
            RunTypeKeywordTest("while");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfSwitchKeywordTyped()
        {
            RunTypeKeywordTest("switch");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfLockKeywordTyped()
        {
            RunTypeKeywordTest("lock");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfUsingKeywordTyped()
        {
            RunTypeKeywordTest("using");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfSectionKeywordTyped()
        {
            RunTypeKeywordTest("section");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfInheritsKeywordTyped()
        {
            RunTypeKeywordTest("inherits");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfFunctionsKeywordTyped()
        {
            RunTypeKeywordTest("functions");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfNamespaceKeywordTyped()
        {
            RunTypeKeywordTest("namespace");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfClassKeywordTyped()
        {
            RunTypeKeywordTest("class");
        }
    }
}
