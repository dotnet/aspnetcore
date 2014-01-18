// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using System.Web.WebPages.TestUtils;
using Microsoft.TestCommon;
using System;

namespace Microsoft.AspNet.Razor.Test.Parser.PartialParsing
{
    public class VBPartialParsingTest : PartialParsingTestBase<VBRazorCodeLanguage>
    {
        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDeleteOfIdentifierPartsIfDotRemains()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @User. baz");
            StringTextBuffer old = new StringTextBuffer("foo @User.Name baz");
            RunPartialParseTest(new TextChange(10, 4, old, 0, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsDeleteOfIdentifierPartsIfSomeOfIdentifierRemains()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @Us baz");
            StringTextBuffer old = new StringTextBuffer("foo @User baz");
            RunPartialParseTest(new TextChange(7, 2, old, 0, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("Us")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsMultipleInsertionIfItCausesIdentifierExpansionAndTrailingDot()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @User. baz");
            StringTextBuffer old = new StringTextBuffer("foo @U baz");
            RunPartialParseTest(new TextChange(6, 0, old, 4, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsMultipleInsertionIfItOnlyCausesIdentifierExpansion()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @barbiz baz");
            StringTextBuffer old = new StringTextBuffer("foo @bar baz");
            RunPartialParseTest(new TextChange(8, 0, old, 3, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("barbiz")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionRejectsChangeWhichWouldHaveBeenAcceptedIfLastChangeWasProvisionallyAcceptedOnDifferentSpan()
        {
            var factory = SpanFactory.CreateVbHtml();

            // Arrange
            TextChange dotTyped = new TextChange(8, 0, new StringTextBuffer("foo @foo @bar"), 1, new StringTextBuffer("foo @foo. @bar"));
            TextChange charTyped = new TextChange(14, 0, new StringTextBuffer("foo @foo. @barb"), 1, new StringTextBuffer("foo @foo. @barb"));
            TestParserManager manager = CreateParserManager();
            manager.InitializeWithDocument(dotTyped.OldBuffer);

            // Apply the dot change
            Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

            // Act (apply the identifier start char change)
            PartialParseResult result = manager.CheckForStructureChangesAndWait(charTyped);

            // Assert
            Assert.Equal(PartialParseResult.Rejected, result);
            Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
            ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree,
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(". "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("barb")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierTypedAfterDotIfLastChangeWasProvisionalAcceptanceOfDot()
        {
            var factory = SpanFactory.CreateVbHtml();

            // Arrange
            TextChange dotTyped = new TextChange(8, 0, new StringTextBuffer("foo @foo bar"), 1, new StringTextBuffer("foo @foo. bar"));
            TextChange charTyped = new TextChange(9, 0, new StringTextBuffer("foo @foo. bar"), 1, new StringTextBuffer("foo @foo.b bar"));
            TestParserManager manager = CreateParserManager();
            manager.InitializeWithDocument(dotTyped.OldBuffer);

            // Apply the dot change
            Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

            // Act (apply the identifier start char change)
            PartialParseResult result = manager.CheckForStructureChangesAndWait(charTyped);

            // Assert
            Assert.Equal(PartialParseResult.Accepted, result);
            Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
            ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree,
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo.b")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" bar")));
        }
        [Fact]
        public void ImplicitExpressionAcceptsIdentifierExpansionAtEndOfNonWhitespaceCharacters()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("@Code" + Environment.NewLine
                                                          + "    @food" + Environment.NewLine
                                                          + "End Code");
            StringTextBuffer old = new StringTextBuffer("@Code" + Environment.NewLine
                                                      + "    @foo" + Environment.NewLine
                                                      + "End Code");
            RunPartialParseTest(new TextChange(15, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("Code")
                               .Accepts(AcceptedCharacters.None),
                        factory.Code("\r\n    ").AsStatement(),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("food")
                                   .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code("\r\n").AsStatement(),
                        factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotAfterIdentifierInMarkup()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @foo. bar");
            StringTextBuffer old = new StringTextBuffer("foo @foo bar");
            RunPartialParseTest(new TextChange(8, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo.")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" bar")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierCharactersIfEndOfSpanIsIdentifier()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("foo @foob baz");
            StringTextBuffer old = new StringTextBuffer("foo @foo bar");
            RunPartialParseTest(new TextChange(8, 0, old, 1, changed),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foob")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" bar")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierStartCharactersIfEndOfSpanIsDot()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("@Code @foo.b End Code");
            StringTextBuffer old = new StringTextBuffer("@Code @foo. End Code");
            RunPartialParseTest(new TextChange(11, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                        factory.Code(" ").AsStatement(),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(" ").AsStatement(),
                        factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotIfTrailingDotsAreAllowed()
        {
            var factory = SpanFactory.CreateVbHtml();
            StringTextBuffer changed = new StringTextBuffer("@Code @foo. End Code");
            StringTextBuffer old = new StringTextBuffer("@Code @foo End Code");
            RunPartialParseTest(new TextChange(10, 0, old, 1, changed),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                        factory.Code(" ").AsStatement(),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.")
                                   .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Code(" ").AsStatement(),
                        factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfFunctionsKeywordTyped()
        {
            RunTypeKeywordTest("functions");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfCodeKeywordTyped()
        {
            RunTypeKeywordTest("code");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfSectionKeywordTyped()
        {
            RunTypeKeywordTest("section");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfDoKeywordTyped()
        {
            RunTypeKeywordTest("do");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfWhileKeywordTyped()
        {
            RunTypeKeywordTest("while");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfIfKeywordTyped()
        {
            RunTypeKeywordTest("if");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfSelectKeywordTyped()
        {
            RunTypeKeywordTest("select");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfForKeywordTyped()
        {
            RunTypeKeywordTest("for");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfTryKeywordTyped()
        {
            RunTypeKeywordTest("try");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfWithKeywordTyped()
        {
            RunTypeKeywordTest("with");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfSyncLockKeywordTyped()
        {
            RunTypeKeywordTest("synclock");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfUsingKeywordTyped()
        {
            RunTypeKeywordTest("using");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfImportsKeywordTyped()
        {
            RunTypeKeywordTest("imports");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfInheritsKeywordTyped()
        {
            RunTypeKeywordTest("inherits");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfOptionKeywordTyped()
        {
            RunTypeKeywordTest("option");
        }

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfHelperKeywordTyped()
        {
            RunTypeKeywordTest("helper");
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

        [Fact]
        public void ImplicitExpressionCorrectlyTriggersReparseIfLayoutKeywordTyped()
        {
            RunTypeKeywordTest("layout");
        }
    }
}