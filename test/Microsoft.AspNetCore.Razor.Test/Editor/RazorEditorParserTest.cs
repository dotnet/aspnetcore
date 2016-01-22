// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Test.Generator;
using Microsoft.AspNetCore.Razor.Test.Utils;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Editor
{
    public class RazorEditorParserTest
    {
        private static readonly TestFile SimpleCSHTMLDocument = TestFile.Create("TestFiles/DesignTime/Simple.cshtml");
        private static readonly TestFile SimpleCSHTMLDocumentGenerated = TestFile.Create("TestFiles/DesignTime/Simple.txt");
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        [Fact]
        public void ConstructorRequiresNonNullPhysicalPath()
        {
            Assert.Throws<ArgumentException>("sourceFileName", () => new RazorEditorParser(CreateHost(), null));
        }

        [Fact]
        public void ConstructorRequiresNonEmptyPhysicalPath()
        {
            Assert.Throws<ArgumentException>("sourceFileName", () => new RazorEditorParser(CreateHost(), string.Empty));
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
                    using (var parser = new RazorEditorParser(CreateHost(), "C:\\Foo.cshtml"))
                    {
                        parser.CheckForStructureChanges(change);
                    }
                });
            ExceptionHelpers.ValidateArgumentException(parameterName, RazorResources.FormatStructure_Member_CannotBeNull(nameof(change.NewBuffer), nameof(TextChange)), exception);
        }

        private static RazorEngineHost CreateHost()
        {
            return new CodeGenTestHost(new CSharpRazorCodeLanguage()) { DesignTimeMode = true };
        }

        [Fact]
        [ReplaceCulture]
        public void CheckForStructureChangesStartsReparseAndFiresDocumentParseCompletedEventIfNoAdditionalChangesQueued()
        {
            // Arrange
            using (var parser = CreateClientParser())
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

        private TextChange CreateDummyChange()
        {
            return new TextChange(0, 0, new StringTextBuffer(string.Empty), 3, new StringTextBuffer("foo"));
        }

        private static RazorEditorParser CreateClientParser()
        {
            return new RazorEditorParser(CreateHost(), TestLinePragmaFileName);
        }
    }
}
