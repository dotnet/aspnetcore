// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class RazorEditorParserTest
    {
        private static readonly TestFile SimpleCSHTMLDocument = TestFile.Create("TestFiles/DesignTime/Simple.cshtml", typeof(RazorEditorParserTest));
        private static readonly TestFile SimpleCSHTMLDocumentGenerated = TestFile.Create("TestFiles/DesignTime/Simple.txt", typeof(RazorEditorParserTest));
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        public static TheoryData TagHelperPartialParseRejectData
        {
            get
            {
                return new TheoryData<TestEdit>
                {
                    CreateInsertionChange("<p></p>", 2, " "),
                    CreateInsertionChange("<p></p>", 6, " "),
                    CreateInsertionChange("<p some-attr></p>", 12, " "),
                    CreateInsertionChange("<p some-attr></p>", 12, "ibute"),
                    CreateInsertionChange("<p some-attr></p>", 2, " before"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperPartialParseRejectData))]
        public void TagHelperTagBodiesRejectPartialChanges(object editObject)
        {
            // Arrange
            var edit = (TestEdit)editObject;
            var builder = TagHelperDescriptorBuilder.Create("PTagHelper", "TestAssembly");
            builder.SetTypeName("PTagHelper");
            builder.TagMatchingRule(rule => rule.TagName = "p");
            var descriptors = new[]
            {
                builder.Build()
            };

            var parser = new RazorEditorParser(CreateTemplateEngine(@"C:\This\Is\A\Test\Path"), @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                var result = manager.CheckForStructureChangesAndWait(edit);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                Assert.Equal(2, manager.ParseCount);
            }
        }

        public static TheoryData TagHelperAttributeAcceptData
        {
            get
            {
                var factory = new SpanFactory();

                // change, (Block)expectedDocument, partialParseResult
                return new TheoryData<TestEdit, PartialParseResult>
                {
                    {
                        CreateInsertionChange("<p str-attr='@DateTime'></p>", 22, "."),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                    {
                        CreateInsertionChange("<p obj-attr='DateTime'></p>", 21, "."),
                        PartialParseResult.Accepted
                    },
                    {
                        CreateInsertionChange("<p obj-attr='1 + DateTime'></p>", 25, "."),
                        PartialParseResult.Accepted
                    },
                    {
                        CreateInsertionChange("<p before-attr str-attr='@DateTime' after-attr></p>", 34, "."),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                    {
                        CreateInsertionChange("<p str-attr='before @DateTime after'></p>", 29, "."),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperAttributeAcceptData))]
        public void TagHelperAttributesAreLocatedAndAcceptChangesCorrectly(object editObject, PartialParseResult partialParseResult)
        {
            // Arrange
            var edit = (TestEdit)editObject;
            var builder = TagHelperDescriptorBuilder.Create("PTagHelper", "Test");
            builder.SetTypeName("PTagHelper");
            builder.TagMatchingRule(rule => rule.TagName = "p");
            builder.BindAttribute(attribute =>
            {
                attribute.Name = "obj-attr";
                attribute.TypeName = typeof(object).FullName;
                attribute.SetPropertyName("ObjectAttribute");
            });
            builder.BindAttribute(attribute =>
            {
                attribute.Name = "str-attr";
                attribute.TypeName = typeof(string).FullName;
                attribute.SetPropertyName("StringAttribute");
            });
            var descriptors = new[] { builder.Build() };

            var parser = new RazorEditorParser(CreateTemplateEngine(@"C:\This\Is\A\Test\Path", descriptors), @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                var result = manager.CheckForStructureChangesAndWait(edit);

                // Assert
                Assert.Equal(partialParseResult, result);
                Assert.Equal(1, manager.ParseCount);
            }
        }

        [Fact]
        public void ConstructorRequiresNonNullPhysicalPath()
        {
            Assert.Throws<ArgumentException>("filePath", () => new RazorEditorParser(CreateTemplateEngine(), null));
        }

        [Fact]
        public void ConstructorRequiresNonEmptyPhysicalPath()
        {
            Assert.Throws<ArgumentException>("filePath", () => new RazorEditorParser(CreateTemplateEngine(), string.Empty));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\r\n")]
        [InlineData("abcdefg")]
        [InlineData("\f\r\n abcd   \t")]
        public void TreesAreDifferentReturnsFalseForAddedContent(string content)
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var original = new MarkupBlock(
                blockFactory.MarkupTagBlock("<p>"),
                blockFactory.TagHelperBlock(
                    tagName: "div",
                    tagMode: TagMode.StartTagAndEndTag,
                    start: new SourceLocation(3, 0, 3),
                    startTag: blockFactory.MarkupTagBlock("<div>"),
                    children: new SyntaxTreeNode[]
                    {
                        factory.Markup($"{Environment.NewLine}{Environment.NewLine}")
                    },
                    endTag: blockFactory.MarkupTagBlock("</div>")),
                blockFactory.MarkupTagBlock("</p>"));

            factory.Reset();

            var modified = new MarkupBlock(
                blockFactory.MarkupTagBlock("<p>"),
                blockFactory.TagHelperBlock(
                    tagName: "div",
                    tagMode: TagMode.StartTagAndEndTag,
                    start: new SourceLocation(3, 0, 3),
                    startTag: blockFactory.MarkupTagBlock("<div>"),
                    children: new SyntaxTreeNode[]
                    {
                        factory.Markup($"{Environment.NewLine}{content}{Environment.NewLine}")
                    },
                    endTag: blockFactory.MarkupTagBlock("</div>")),
                blockFactory.MarkupTagBlock("</p>"));
            original.LinkNodes();
            modified.LinkNodes();

            // Act
            var treesAreDifferent = RazorEditorParser.BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new SourceChange(
                        absoluteIndex: 8 + Environment.NewLine.Length,
                        length: 0,
                        newText: content)
                },
                CancellationToken.None);

            // Assert
            Assert.False(treesAreDifferent);
        }

        [Fact]
        public void TreesAreDifferentReturnsTrueIfTreeStructureIsDifferent()
        {
            var factory = new SpanFactory();
            var original = new MarkupBlock(
                factory.Markup("<p>"),
                new ExpressionBlock(
                    factory.CodeTransition()),
                factory.Markup("</p>"));
            var modified = new MarkupBlock(
                factory.Markup("<p>"),
                new ExpressionBlock(
                    factory.CodeTransition("@"),
                    factory.Code("f")
                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)),
                factory.Markup("</p>"));
            Assert.True(RazorEditorParser.BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new SourceChange(absoluteIndex: 4, length: 0, newText: "f")
                },
                CancellationToken.None));
        }

        [Fact]
        public void TreesAreDifferentReturnsFalseIfTreeStructureIsSame()
        {
            var factory = new SpanFactory();
            var original = new MarkupBlock(
                factory.Markup("<p>"),
                new ExpressionBlock(
                    factory.CodeTransition(),
                    factory.Code("f")
                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)),
                factory.Markup("</p>"));
            factory.Reset();
            var modified = new MarkupBlock(
                factory.Markup("<p>"),
                new ExpressionBlock(
                    factory.CodeTransition(),
                    factory.Code("foo")
                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)),
                factory.Markup("</p>"));
            original.LinkNodes();
            modified.LinkNodes();
            Assert.False(RazorEditorParser.BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new SourceChange(absoluteIndex: 5, length: 0, newText: "oo")
                },
                CancellationToken.None));
        }

        [Fact]
        public void CheckForStructureChangesStartsFullReparseIfChangeOverlapsMultipleSpans()
        {
            // Arrange
            using (var parser = new RazorEditorParser(CreateTemplateEngine(), TestLinePragmaFileName))
            {
                var original = new StringTextSnapshot("Foo @bar Baz");
                var changed = new StringTextSnapshot("Foo @bap Daz");
                var change = new SourceChange(7, 3, "p D");

                var parseComplete = new ManualResetEventSlim();
                var parseCount = 0;
                parser.DocumentParseComplete += (sender, args) =>
                {
                    Interlocked.Increment(ref parseCount);
                    parseComplete.Set();
                };

                Assert.Equal(PartialParseResult.Rejected, parser.CheckForStructureChanges(change, original));
                DoWithTimeoutIfNotDebugging(parseComplete.Wait); // Wait for the parse to finish
                parseComplete.Reset();

                // Act
                var result = parser.CheckForStructureChanges(change, original);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                DoWithTimeoutIfNotDebugging(parseComplete.Wait);
                Assert.Equal(2, parseCount);
            }
        }

        [Fact]
        public void AwaitPeriodInsertionAcceptedProvisionally()
        {
            // Arrange
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @await Html. baz");
            var old = new StringTextSnapshot("foo @await Html baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(15, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("await Html.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.WhiteSpace | AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")), additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertionsInStatementBlock()
        {
            // Arrange
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime..Now" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime.Now" + Environment.NewLine
                                                + "}");

            // Act and Assert
            RunPartialParseTest(new TestEdit(17, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime..Now")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertions()
        {
            // Arrange
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateTime..Now baz");
            var old = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(13, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime..Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")), additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsWholeIdentifierReplacement()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 4, old, 8, changed, "DateTime"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionRejectsWholeIdentifierReplacementToKeyword()
        {
            // Arrange
            var parser = new RazorEditorParser(CreateTemplateEngine(@"C:\This\Is\A\Test\Path"), @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                var old = new StringTextSnapshot("foo @date baz");
                var changed = new StringTextSnapshot("foo @if baz");
                var edit = new TestEdit(5, 4, old, 2, changed, "if");
                manager.InitializeWithDocument(old);

                // Act
                var result = manager.CheckForStructureChangesAndWait(edit);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                Assert.Equal(2, manager.ParseCount);
            }
        }

        [Fact]
        public void ImplicitExpressionRejectsWholeIdentifierReplacementToDirective()
        {
            // Arrange
            var parser = new RazorEditorParser(CreateTemplateEngine(@"C:\This\Is\A\Test\Path"), @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                var old = new StringTextSnapshot("foo @date baz");
                var changed = new StringTextSnapshot("foo @inherits baz");
                var SourceChange = new TestEdit(5, 4, old, 8, changed, "inherits");
                manager.InitializeWithDocument(old);

                // Act
                var result = manager.CheckForStructureChangesAndWait(SourceChange);

                // Assert
                Assert.Equal(PartialParseResult.Rejected | PartialParseResult.SpanContextChanged, result);
                Assert.Equal(2, manager.ParseCount);
            }
        }

        [Fact]
        public void ImplicitExpressionAcceptsPrefixIdentifierReplacements_SingleSymbol()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @dTime baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 1, old, 4, changed, "Date"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsPrefixIdentifierReplacements_MultipleSymbols()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @dTime.Now baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 1, old, 4, changed, "Date"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSuffixIdentifierReplacements_SingleSymbol()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @Datet baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(9, 1, old, 4, changed, "Time"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSuffixIdentifierReplacements_MultipleSymbols()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @DateTime.n baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(14, 1, old, 3, changed, "Now"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSurroundedIdentifierReplacements()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @DateTime.n.ToString() baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now.ToString() baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(14, 1, old, 3, changed, "Now"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now.ToString()").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlockAfterIdentifiers()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime." + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime" + Environment.NewLine
                                                + "}");

            var edit = new TestEdit(15 + Environment.NewLine.Length, 0, old, 1, changed, ".");
            using (var manager = CreateParserManager())
            {
                Action<TestEdit, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(edit);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);
                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateTime.");

                old = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime.." + Environment.NewLine
                                                    + "}");
                edit = new TestEdit(16 + Environment.NewLine.Length, 0, old, 1, changed, ".");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateTime..");

                old = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime.Now." + Environment.NewLine
                                                    + "}");
                edit = new TestEdit(16 + Environment.NewLine.Length, 0, old, 3, changed, "Now");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateTime.Now.");
            }
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlock()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateT." + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateT" + Environment.NewLine
                                                + "}");

            var edit = new TestEdit(12 + Environment.NewLine.Length, 0, old, 1, changed, ".");
            using (var manager = CreateParserManager())
            {
                Action<TestEdit, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(edit);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);
                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateT.");

                old = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime." + Environment.NewLine
                                                    + "}");
                edit = new TestEdit(12 + Environment.NewLine.Length, 0, old, 3, changed, "ime");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateTime.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertions()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateT. baz");
            var old = new StringTextSnapshot("foo @DateT baz");
            var edit = new TestEdit(10, 0, old, 1, changed, ".");
            using (var manager = CreateParserManager())
            {
                Action<TestEdit, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(edit);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateT.");

                old = changed;
                changed = new StringTextSnapshot("foo @DateTime. baz");
                edit = new TestEdit(10, 0, old, 3, changed, "ime");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertionsAfterIdentifiers()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateTime. baz");
            var old = new StringTextSnapshot("foo @DateTime baz");
            var edit = new TestEdit(13, 0, old, 1, changed, ".");
            using (var manager = CreateParserManager())
            {
                Action<TestEdit, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(edit);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.");

                old = changed;
                changed = new StringTextSnapshot("foo @DateTime.. baz");
                edit = new TestEdit(14, 0, old, 1, changed, ".");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime..");

                old = changed;
                changed = new StringTextSnapshot("foo @DateTime.Now. baz");
                edit = new TestEdit(14, 0, old, 3, changed, "Now");

                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.Now.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsCaseInsensitiveDotlessCommitInsertions_NewRoslynIntegration()
        {
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @date. baz");
            var edit = new TestEdit(9, 0, old, 1, changed, ".");
            using (var manager = CreateParserManager())
            {
                Action<TestEdit, PartialParseResult, string> applyAndVerifyPartialChange = (changeToApply, expectedResult, expectedCode) =>
                {
                    var result = manager.CheckForStructureChangesAndWait(edit);

                    // Assert
                    Assert.Equal(expectedResult, result);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.

                // @date => @date.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "date.");

                old = changed;
                changed = new StringTextSnapshot("foo @date baz");
                edit = new TestEdit(9, 1, old, 0, changed, "");

                // @date. => @date
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "date");

                old = changed;
                changed = new StringTextSnapshot("foo @DateTime baz");
                edit = new TestEdit(5, 4, old, 8, changed, "DateTime");

                // @date => @DateTime
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted, "DateTime");

                old = changed;
                changed = new StringTextSnapshot("foo @DateTime. baz");
                edit = new TestEdit(13, 0, old, 1, changed, ".");

                // @DateTime => @DateTime.
                applyAndVerifyPartialChange(edit, PartialParseResult.Accepted | PartialParseResult.Provisional, "DateTime.");
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDeleteOfIdentifierPartsIfDotRemains()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @User. baz");
            var old = new StringTextSnapshot("foo @User.Name baz");
            RunPartialParseTest(new TestEdit(10, 4, old, 0, changed, string.Empty),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsDeleteOfIdentifierPartsIfSomeOfIdentifierRemains()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @Us baz");
            var old = new StringTextSnapshot("foo @User baz");
            RunPartialParseTest(new TestEdit(7, 2, old, 0, changed, string.Empty),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("Us").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsMultipleInsertionIfItCausesIdentifierExpansionAndTrailingDot()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @User. baz");
            var old = new StringTextSnapshot("foo @U baz");
            RunPartialParseTest(new TestEdit(6, 0, old, 4, changed, "ser."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsMultipleInsertionIfItOnlyCausesIdentifierExpansion()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @barbiz baz");
            var old = new StringTextSnapshot("foo @bar baz");
            RunPartialParseTest(new TestEdit(8, 0, old, 3, changed, "biz"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("barbiz").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierExpansionAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @food" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(10 + Environment.NewLine.Length, 0, old, 1, changed, "d"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("food")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierAfterDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @foo.d" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo." + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(11 + Environment.NewLine.Length, 0, old, 1, changed, "d"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.d")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @foo." + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(10 + Environment.NewLine.Length, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(@"foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionRejectsChangeWhichWouldHaveBeenAcceptedIfLastChangeWasProvisionallyAcceptedOnDifferentSpan()
        {
            var factory = new SpanFactory();

            // Arrange
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo @bar"), 1, new StringTextSnapshot("foo @foo. @bar"), ".");
            var charTyped = new TestEdit(14, 0, new StringTextSnapshot("foo @foo. @bar"), 1, new StringTextSnapshot("foo @foo. @barb"), "b");
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(dotTyped.OldSnapshot);

                // Apply the dot change
                Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

                // Act (apply the identifier start char change)
                var result = manager.CheckForStructureChangesAndWait(charTyped);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(". "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("barb")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyHtml()));
            }
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierTypedAfterDotIfLastChangeWasProvisionalAcceptanceOfDot()
        {
            var factory = new SpanFactory();

            // Arrange
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo bar"), 1, new StringTextSnapshot("foo @foo. bar"), ".");
            var charTyped = new TestEdit(9, 0, new StringTextSnapshot("foo @foo. bar"), 1, new StringTextSnapshot("foo @foo.b bar"), "b");
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(dotTyped.OldSnapshot);

                // Apply the dot change
                Assert.Equal(PartialParseResult.Provisional | PartialParseResult.Accepted, manager.CheckForStructureChangesAndWait(dotTyped));

                // Act (apply the identifier start char change)
                var result = manager.CheckForStructureChangesAndWait(charTyped);

                // Assert
                Assert.Equal(PartialParseResult.Accepted, result);
                Assert.False(manager.Parser.LastResultProvisional, "LastResultProvisional flag should have been cleared but it was not");
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" bar")));
            }
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotAfterIdentifierInMarkup()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @foo. bar");
            var old = new StringTextSnapshot("foo @foo bar");
            RunPartialParseTest(new TestEdit(8, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo.")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" bar")),
                additionalFlags: PartialParseResult.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierCharactersIfEndOfSpanIsIdentifier()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @foob bar");
            var old = new StringTextSnapshot("foo @foo bar");
            RunPartialParseTest(new TestEdit(8, 0, old, 1, changed, "b"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foob")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" bar")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierStartCharactersIfEndOfSpanIsDot()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{@foo.b}");
            var old = new StringTextSnapshot("@{@foo.}");
            RunPartialParseTest(new TestEdit(7, 0, old, 1, changed, "b"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotIfTrailingDotsAreAllowed()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{@foo.}");
            var old = new StringTextSnapshot("@{@foo}");
            RunPartialParseTest(new TestEdit(6, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
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

        private static TestEdit CreateInsertionChange(string initialText, int insertionLocation, string insertionText)
        {
            var changedText = initialText.Insert(insertionLocation, insertionText);
            var sourceChange = new SourceChange(insertionLocation, 0, insertionText);
            var oldSnapshot = new StringTextSnapshot(initialText);
            var changedSnapshot = new StringTextSnapshot(changedText);
            return new TestEdit
            {
                Change = sourceChange,
                OldSnapshot = oldSnapshot,
                NewSnapshot = changedSnapshot,
            };
        }

        private static void RunFullReparseTest(TestEdit edit, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                var result = manager.CheckForStructureChangesAndWait(edit);

                // Assert
                Assert.Equal(PartialParseResult.Rejected | additionalFlags, result);
                Assert.Equal(2, manager.ParseCount);
            }
        }

        private static void RunPartialParseTest(TestEdit edit, Block newTreeRoot, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                var result = manager.CheckForStructureChangesAndWait(edit);

                // Assert
                Assert.Equal(PartialParseResult.Accepted | additionalFlags, result);
                Assert.Equal(1, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentSyntaxTree.Root, newTreeRoot);
            }
        }

        private static TestParserManager CreateParserManager()
        {
            var parser = new RazorEditorParser(CreateTemplateEngine(), TestLinePragmaFileName);
            return new TestParserManager(parser);
        }

        private static RazorTemplateEngine CreateTemplateEngine(
            string path = TestLinePragmaFileName,
            IEnumerable<TagHelperDescriptor> tagHelpers = null)
        {
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                RazorExtensions.Register(builder);

                if (tagHelpers != null)
                {
                    builder.AddTagHelpers(tagHelpers);
                }
            });

            // GetImports on RazorTemplateEngine will at least check that the item exists, so we need to pretend
            // that it does.
            var items = new List<RazorProjectItem>();
            items.Add(new TestRazorProjectItem(path));

            var project = new TestRazorProject(items);

            var templateEngine = new RazorTemplateEngine(engine, project);
            templateEngine.Options.DefaultImports = RazorSourceDocument.Create("@addTagHelper *, Test", "_TestImports.cshtml");
            return templateEngine;
        }

        private static void RunTypeKeywordTest(string keyword)
        {
            var before = "@" + keyword.Substring(0, keyword.Length - 1);
            var after = "@" + keyword;
            var changed = new StringTextSnapshot(after);
            var old = new StringTextSnapshot(before);
            var change = new SourceChange(keyword.Length, 0, keyword[keyword.Length - 1].ToString());
            var edit = new TestEdit
            {
                Change = change,
                NewSnapshot = changed,
                OldSnapshot = old
            };
            RunFullReparseTest(edit, additionalFlags: PartialParseResult.SpanContextChanged);
        }

        private static void DoWithTimeoutIfNotDebugging(Func<int, bool> withTimeout)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                withTimeout(Timeout.Infinite);
            }
            else
            {
#endif
                Assert.True(withTimeout((int)TimeSpan.FromSeconds(1).TotalMilliseconds), "Timeout expired!");
#if DEBUG
            }
#endif
        }

        private class TestParserManager : IDisposable
        {
            public int ParseCount;

            private readonly ManualResetEventSlim _parserComplete;

            public TestParserManager(RazorEditorParser parser)
            {
                _parserComplete = new ManualResetEventSlim();
                ParseCount = 0;
                Parser = parser;
                parser.DocumentParseComplete += (sender, args) =>
                {
                    Interlocked.Increment(ref ParseCount);
                    _parserComplete.Set();
                };
            }

            public RazorEditorParser Parser { get; }

            public void InitializeWithDocument(ITextSnapshot snapshot)
            {
                var initialChange = new SourceChange(0, 0, string.Empty);
                var edit = new TestEdit
                {
                    Change = initialChange,
                    OldSnapshot = snapshot,
                    NewSnapshot = snapshot
                };
                CheckForStructureChangesAndWait(edit);
            }

            public PartialParseResult CheckForStructureChangesAndWait(TestEdit edit)
            {
                var result = Parser.CheckForStructureChanges(edit.Change, edit.NewSnapshot);
                if (result.HasFlag(PartialParseResult.Rejected))
                {
                    WaitForParse();
                }
                return result;
            }

            public void WaitForParse()
            {
                DoWithTimeoutIfNotDebugging(_parserComplete.Wait); // Wait for the parse to finish
                _parserComplete.Reset();
            }

            public void Dispose()
            {
                Parser.Dispose();
            }
        }

        private class TestEdit
        {
            public TestEdit()
            {
            }

            public TestEdit(int position, int oldLength, ITextSnapshot oldSnapshot, int newLength, ITextSnapshot newSnapshot, string newText)
            {
                Change = new SourceChange(position, oldLength, newText);
                OldSnapshot = oldSnapshot;
                NewSnapshot = newSnapshot;
            }

            public SourceChange Change { get; set; }

            public ITextSnapshot OldSnapshot { get; set; }

            public ITextSnapshot NewSnapshot { get; set; }
        }
    }
}