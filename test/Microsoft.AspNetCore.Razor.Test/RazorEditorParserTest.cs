// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Parser.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Test.CodeGenerators;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Test.Utils;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor
{
    public class RazorEditorParserTest : PartialParsingTestBase<CSharpRazorCodeLanguage>
    {
        private static readonly TestFile SimpleCSHTMLDocument = TestFile.Create("TestFiles/DesignTime/Simple.cshtml");
        private static readonly TestFile SimpleCSHTMLDocumentGenerated = TestFile.Create("TestFiles/DesignTime/Simple.txt");
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        public static TheoryData TagHelperPartialParseRejectData
        {
            get
            {
                var factory = SpanFactory.CreateCsHtml();

                // change, expectedDocument
                return new TheoryData<TextChange, MarkupBlock>
                {
                    {
                        CreateInsertionChange("<p></p>", 2, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p"))
                    },
                    {
                        CreateInsertionChange("<p></p>", 6, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p"))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 12, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "some-attr",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized)
                                }))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 12, "ibute"),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "some-attribute",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized)
                                }))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 2, " before"),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "before",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "some-attr",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized)
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperPartialParseRejectData))]
        public void TagHelperTagBodiesRejectPartialChanges(TextChange change, MarkupBlock expectedDocument)
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "p",
                    TypeName = "PTagHelper"
                },
            };
            var descriptorResolver = new Mock<ITagHelperDescriptorResolver>();
            descriptorResolver
                .Setup(resolver => resolver.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                .Returns(descriptors);
            var host = CreateHost(descriptorResolver.Object);
            var parser = new RazorEditorParser(host, @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                manager.InitializeWithDocument(change.OldBuffer);

                // Act
                var result = manager.CheckForStructureChangesAndWait(change);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, expectedDocument);
            }
        }

        public static TheoryData TagHelperAttributeAcceptData
        {
            get
            {
                var factory = SpanFactory.CreateCsHtml();

                // change, expectedDocument, partialParseResult
                return new TheoryData<TextChange, MarkupBlock, PartialParseResult>
                {
                    {
                        CreateInsertionChange("<p str-attr='@DateTime'></p>", 22, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                    {
                        CreateInsertionChange("<p obj-attr='DateTime'></p>", 21, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "obj-attr",
                                        factory.CodeMarkup("DateTime."),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        PartialParseResult.Accepted
                    },
                    {
                        CreateInsertionChange("<p obj-attr='1 + DateTime'></p>", 25, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "obj-attr",
                                        factory.CodeMarkup("1 + DateTime."),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        PartialParseResult.Accepted
                    },
                    {
                        CreateInsertionChange("<p before-attr str-attr='@DateTime' after-attr></p>", 34, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "before-attr",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "after-attr",
                                        value: null,
                                        valueStyle: HtmlAttributeValueStyle.Minimized),
                                })),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                    {
                        CreateInsertionChange("<p str-attr='before @DateTime after'></p>", 29, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            factory.Markup("before"),
                                            new MarkupBlock(
                                                factory.Markup(" "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                            factory.Markup(" after")),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        PartialParseResult.Accepted | PartialParseResult.Provisional
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperAttributeAcceptData))]
        public void TagHelperAttributesAreLocatedAndAcceptChangesCorrectly(
            TextChange change,
            MarkupBlock expectedDocument,
            PartialParseResult partialParseResult)
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "p",
                    TypeName = "PTagHelper",
                    Attributes = new[]
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "obj-attr",
                            TypeName = typeof(object).FullName,
                            PropertyName = "ObjectAttribute",
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "str-attr",
                            TypeName = typeof(string).FullName,
                            PropertyName = "StringAttribute",
                        },
                    }
                },
            };
            var descriptorResolver = new Mock<ITagHelperDescriptorResolver>();
            descriptorResolver
                .Setup(resolver => resolver.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                .Returns(descriptors);
            var host = CreateHost(descriptorResolver.Object);
            var parser = new RazorEditorParser(host, @"C:\This\Is\A\Test\Path");

            using (var manager = new TestParserManager(parser))
            {
                manager.InitializeWithDocument(change.OldBuffer);

                // Act
                var result = manager.CheckForStructureChangesAndWait(change);

                // Assert
                Assert.Equal(partialParseResult, result);
                Assert.Equal(1, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, expectedDocument);
            }
        }

        [Fact]
        public void ConstructorRequiresNonNullPhysicalPath()
        {
            Assert.Throws<ArgumentException>("sourceFileName", () => new RazorEditorParser(CreateCodeGenTestHost(), null));
        }

        [Fact]
        public void ConstructorRequiresNonEmptyPhysicalPath()
        {
            Assert.Throws<ArgumentException>("sourceFileName", () => new RazorEditorParser(CreateCodeGenTestHost(), string.Empty));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\r\n")]
        [InlineData("abcdefg")]
        [InlineData("\f\r\n abcd   \t")]
        public void TreesAreDifferentReturnsFalseForAddedContent(string content)
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
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

            var oldBuffer = new StringTextBuffer($"<p><div>{Environment.NewLine}{Environment.NewLine}</div></p>");
            var newBuffer = new StringTextBuffer(
                $"<p><div>{Environment.NewLine}{content}{Environment.NewLine}</div></p>");

            // Act
            var treesAreDifferent = BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new TextChange(
                        position: 8 + Environment.NewLine.Length,
                        oldLength: 0,
                        oldBuffer: oldBuffer,
                        newLength: content.Length,
                        newBuffer: newBuffer)
                });

            // Assert
            Assert.False(treesAreDifferent);
        }

        [Fact]
        public void TreesAreDifferentReturnsTrueIfTreeStructureIsDifferent()
        {
            var factory = SpanFactory.CreateCsHtml();
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
            var oldBuffer = new StringTextBuffer("<p>@</p>");
            var newBuffer = new StringTextBuffer("<p>@f</p>");
            Assert.True(BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new TextChange(position: 4, oldLength: 0, oldBuffer: oldBuffer, newLength: 1, newBuffer: newBuffer)
                }));
        }

        [Fact]
        public void TreesAreDifferentReturnsFalseIfTreeStructureIsSame()
        {
            var factory = SpanFactory.CreateCsHtml();
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
            var oldBuffer = new StringTextBuffer("<p>@f</p>");
            var newBuffer = new StringTextBuffer("<p>@foo</p>");
            Assert.False(BackgroundParser.TreesAreDifferent(
                original,
                modified,
                new[]
                {
                    new TextChange(position: 5, oldLength: 0, oldBuffer: oldBuffer, newLength: 2, newBuffer: newBuffer)
                }));
        }

        [Fact]
        public void CheckForStructureChangesRequiresNonNullBufferInChange()
        {
            var change = new TextChange();
            var parameterName = "change";
            var exception = Assert.Throws<ArgumentException>(
                parameterName,
                () =>
                {
                    using (var parser = new RazorEditorParser(CreateCodeGenTestHost(), "C:\\Foo.cshtml"))
                    {
                        parser.CheckForStructureChanges(change);
                    }
                });
            ExceptionHelpers.ValidateArgumentException(parameterName, RazorResources.FormatStructure_Member_CannotBeNull(nameof(change.NewBuffer), nameof(TextChange)), exception);
        }

        [Fact]
        [ReplaceCulture]
        public void CheckForStructureChangesStartsReparseAndFiresDocumentParseCompletedEventIfNoAdditionalChangesQueued()
        {
            // Arrange
            using (var parser = new RazorEditorParser(CreateCodeGenTestHost(), TestLinePragmaFileName))
            {
                var input = new StringTextBuffer(SimpleCSHTMLDocument.ReadAllText());

                DocumentParseCompleteEventArgs capturedArgs = null;
                var parseComplete = new ManualResetEventSlim(false);

                parser.DocumentParseComplete += (sender, args) =>
                {
                    capturedArgs = args;
                    parseComplete.Set();
                };

                // Act
                parser.CheckForStructureChanges(new TextChange(0, 0, new StringTextBuffer(string.Empty), input.Length, input));

                // Assert
                MiscUtils.DoWithTimeoutIfNotDebugging(parseComplete.Wait);

                Assert.Equal(
                    SimpleCSHTMLDocumentGenerated.ReadAllText(),
                    capturedArgs.GeneratorResults.GeneratedCode);
            }
        }

        [Fact]
        public void CheckForStructureChangesStartsFullReparseIfChangeOverlapsMultipleSpans()
        {
            // Arrange
            using (var parser = new RazorEditorParser(CreateHost(), TestLinePragmaFileName))
            {
                var original = new StringTextBuffer("Foo @bar Baz");
                var changed = new StringTextBuffer("Foo @bap Daz");
                var change = new TextChange(7, 3, original, 3, changed);

                var parseComplete = new ManualResetEventSlim();
                var parseCount = 0;
                parser.DocumentParseComplete += (sender, args) =>
                {
                    Interlocked.Increment(ref parseCount);
                    parseComplete.Set();
                };

                Assert.Equal(PartialParseResult.Rejected, parser.CheckForStructureChanges(new TextChange(0, 0, new StringTextBuffer(string.Empty), 12, original)));
                MiscUtils.DoWithTimeoutIfNotDebugging(parseComplete.Wait); // Wait for the parse to finish
                parseComplete.Reset();

                // Act
                var result = parser.CheckForStructureChanges(change);

                // Assert
                Assert.Equal(PartialParseResult.Rejected, result);
                MiscUtils.DoWithTimeoutIfNotDebugging(parseComplete.Wait);
                Assert.Equal(2, parseCount);
            }
        }

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

        private static RazorEngineHost CreateCodeGenTestHost()
        {
            return new CodeGenTestHost(new CSharpRazorCodeLanguage()) { DesignTimeMode = true };
        }

        private static TextChange CreateInsertionChange(string initialText, int insertionLocation, string insertionText)
        {
            var changedText = initialText.Insert(insertionLocation, insertionText);

            var original = new StringTextBuffer(initialText);
            var changed = new StringTextBuffer(changedText);
            return new TextChange(insertionLocation, 0, original, insertionText.Length, changed);
        }
    }
}
