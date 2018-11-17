// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Xunit;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class BraceSmartIndenterTest : BraceSmartIndenterTestBase
    {
        [Fact]
        public void AtApplicableRazorBlock_NestedIfBlock_ReturnsFalse()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("@{ if (true) { } }");

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(14, syntaxTree);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_SectionBlock_ReturnsTrue()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("@section Foo { }");

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(15, syntaxTree);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_FunctionsBlock_ReturnsTrue()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("@functions { }");

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(13, syntaxTree);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_ExplicitCodeBlock_ReturnsTrue()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("@{ }");

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(3, syntaxTree);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsInvalidContent_NewLineSpan_ReturnsFalse()
        {
            // Arrange
            var span = ExtractSpan(2, "@{" + Environment.NewLine + "}");

            // Act
            var result = BraceSmartIndenter.ContainsInvalidContent(span);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsInvalidContent_WhitespaceSpan_ReturnsFalse()
        {
            // Arrange
            var span = ExtractSpan(2, "@{ }");

            // Act
            var result = BraceSmartIndenter.ContainsInvalidContent(span);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsInvalidContent_MarkerSpan_ReturnsFalse()
        {
            // Arrange
            var span = ExtractSpan(3, "@{}");

            // Act
            var result = BraceSmartIndenter.ContainsInvalidContent(span);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsInvalidContent_NonWhitespaceMarker_ReturnsTrue()
        {
            // Arrange
            var span = ExtractSpan(2, "@{ if}");

            // Act
            var result = BraceSmartIndenter.ContainsInvalidContent(span);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUnlinkedSpan_NullPrevious_ReturnsTrue()
        {
            // Arrange
            var span = ExtractSpan(0, "@{}");

            // Act
            var result = BraceSmartIndenter.IsUnlinkedSpan(span);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUnlinkedSpan_NullNext_ReturnsTrue()
        {
            // Arrange
            var span = ExtractSpan(3, "@{}");

            // Act
            var result = BraceSmartIndenter.IsUnlinkedSpan(span);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUnlinkedSpan_NullOwner_ReturnsTrue()
        {
            // Arrange
            SyntaxNode owner = null;

            // Act
            var result = BraceSmartIndenter.IsUnlinkedSpan(owner);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SurroundedByInvalidContent_MetacodeSurroundings_ReturnsFalse()
        {
            // Arrange
            var span = ExtractSpan(2, "@{}");

            // Act
            var result = BraceSmartIndenter.SurroundedByInvalidContent(span);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SurroundedByInvalidContent_OnlyNextMetacode_ReturnsTrue()
        {
            // Arrange
            var span = ExtractSpan(9, "@{<p></p>}");

            // Act
            var result = BraceSmartIndenter.SurroundedByInvalidContent(span);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SurroundedByInvalidContent_OnlyPreviousMetacode_ReturnsTrue()
        {
            // Arrange
            var span = ExtractSpan(2, "@{<p>");

            // Act
            var result = BraceSmartIndenter.SurroundedByInvalidContent(span);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_AtMarkup_ReturnsFalse()
        {
            // Arrange
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("<p></p>"));
            var changePosition = 2;

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(changePosition, syntaxTree);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_AtExplicitCodeBlocksCode_ReturnsTrue()
        {
            // Arrange
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@{}"));
            var changePosition = 2;

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(changePosition, syntaxTree);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_AtMetacode_ReturnsTrue()
        {
            // Arrange
            var parseOptions = RazorParserOptions.Create(options => options.Directives.Add(FunctionsDirective.Directive));
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@functions {}"), parseOptions);
            var changePosition = 12;

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(changePosition, syntaxTree);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AtApplicableRazorBlock_WhenNoOwner_ReturnsFalse()
        {
            // Arrange
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@DateTime.Now"));
            var changePosition = 14; // 1 after the end of the content

            // Act
            var result = BraceSmartIndenter.AtApplicableRazorBlock(changePosition, syntaxTree);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertIndent_InsertsProvidedIndentIntoBuffer()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ \n}");
            var expectedIndentResult = "@{ anything\n}";
            ITextBuffer textBuffer = null;
            var textView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, textView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            BraceSmartIndenter.InsertIndent(3, "anything", textBuffer);

            // Assert
            Assert.Equal(expectedIndentResult, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }

        [Fact]
        public void RestoreCaretTo_PlacesCursorAtProvidedPosition()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ \n\n}");
            var bufferPosition = new VirtualSnapshotPoint(initialSnapshot, 4);
            var caret = new Mock<ITextCaret>();
            caret.Setup(c => c.MoveTo(It.IsAny<SnapshotPoint>()))
                .Callback<SnapshotPoint>(point =>
                {
                    Assert.Equal(3, point.Position);
                    Assert.Same(initialSnapshot, point.Snapshot);
                });
            ITextBuffer textBuffer = null;
            var textView = CreateFocusedTextView(() => textBuffer, caret.Object);
            var documentTracker = CreateDocumentTracker(() => textBuffer, textView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            BraceSmartIndenter.RestoreCaretTo(3, textView);

            // Assert
            caret.VerifyAll();
        }

        [Fact]
        public void TriggerSmartIndent_ForcesEditorToMoveToEndOfLine()
        {
            // Arrange
            var textView = CreateFocusedTextView();
            var editorOperations = new Mock<IEditorOperations>();
            editorOperations.Setup(operations => operations.MoveToEndOfLine(false));
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var documentTracker = CreateDocumentTracker(() => Mock.Of<ITextBuffer>(), textView);
            editorOperationsFactory.Setup(factory => factory.GetEditorOperations(textView))
                .Returns(editorOperations.Object);
            var codeDocumentProvider = Mock.Of<TextBufferCodeDocumentProvider>();
            var smartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory.Object);

            // Act
            smartIndenter.TriggerSmartIndent(textView);

            // Assert
            editorOperations.VerifyAll();
        }

        [Fact]
        public void AfterClosingBrace_ContentAfterBrace_ReturnsFalse()
        {
            // Arrange
            var fileSnapshot = new StringTextSnapshot("@functions\n{a\n}");
            var changePosition = 13;
            var line = fileSnapshot.GetLineFromPosition(changePosition);

            // Act & Assert
            Assert.False(BraceSmartIndenter.BeforeClosingBrace(0, line));
        }

        [Theory]
        [InlineData("@functions\n{\n}")]
        [InlineData("@functions\n{   \n}")]
        [InlineData("@functions\n  {   \n}")]
        [InlineData("@functions\n\t\t{\t\t\n}")]
        public void AfterClosingBrace_BraceBeforePosition_ReturnsTrue(string fileContent)
        {
            // Arrange
            var fileSnapshot = new StringTextSnapshot(fileContent);
            var changePosition = fileContent.Length - 3 /* \n} */;
            var line = fileSnapshot.GetLineFromPosition(changePosition);

            // Act & Assert
            Assert.True(BraceSmartIndenter.AfterOpeningBrace(line.Length - 1, line));
        }

        [Fact]
        public void BeforeClosingBrace_ContentPriorToBrace_ReturnsFalse()
        {
            // Arrange
            var fileSnapshot = new StringTextSnapshot("@functions\n{\na}");
            var changePosition = 12;
            var line = fileSnapshot.GetLineFromPosition(changePosition + 1 /* \n */);

            // Act & Assert
            Assert.False(BraceSmartIndenter.BeforeClosingBrace(0, line));
        }

        [Theory]
        [InlineData("@functions\n{\n}")]
        [InlineData("@functions\n{\n   }")]
        [InlineData("@functions\n{\n   }   ")]
        [InlineData("@functions\n{\n\t\t   }   ")]
        public void BeforeClosingBrace_BraceAfterPosition_ReturnsTrue(string fileContent)
        {
            // Arrange
            var fileSnapshot = new StringTextSnapshot(fileContent);
            var changePosition = 12;
            var line = fileSnapshot.GetLineFromPosition(changePosition + 1 /* \n */);

            // Act & Assert
            Assert.True(BraceSmartIndenter.BeforeClosingBrace(0, line));
        }

        [ForegroundFact]
        public void TextBuffer_OnChanged_NoopsIfNoChanges()
        {
            // Arrange
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var changeCollection = new TestTextChangeCollection();
            var textContentChangeArgs = new TestTextContentChangedEventArgs(changeCollection);
            var documentTracker = CreateDocumentTracker(() => Mock.Of<ITextBuffer>(), Mock.Of<ITextView>());
            var codeDocumentProvider = Mock.Of<TextBufferCodeDocumentProvider>();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory.Object);

            // Act & Assert
            braceSmartIndenter.TextBuffer_OnChanged(null, textContentChangeArgs);
        }

        [ForegroundFact]
        public void TextBuffer_OnChanged_NoopsIfChangesThatResultInNoChange()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("Hello World");
            var textBuffer = new TestTextBuffer(initialSnapshot);
            var edit = new TestEdit(0, 0, initialSnapshot, 0, initialSnapshot, string.Empty);
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var documentTracker = CreateDocumentTracker(() => textBuffer, Mock.Of<ITextView>());
            var codeDocumentProvider = Mock.Of<TextBufferCodeDocumentProvider>();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory.Object);

            // Act & Assert
            textBuffer.ApplyEdits(edit, edit);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNoFocusedTextView()
        {
            // Arrange
            var snapshot = new StringTextSnapshot(Environment.NewLine + "Hello World");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(snapshot.Content));
            ITextBuffer textBuffer = null;
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView: null);
            textBuffer = CreateTextBuffer(snapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, Environment.NewLine.Length, Environment.NewLine, syntaxTree, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfTextChangeIsNotNewline()
        {
            // Arrange
            var snapshot = new StringTextSnapshot("This Hello World");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(snapshot.Content));
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(snapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, 5, "This ", syntaxTree, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotPrecededByOpenBrace_FileStart()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot(Environment.NewLine + "Hello World");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(initialSnapshot.Content));
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, Environment.NewLine.Length, Environment.NewLine, syntaxTree, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotPrecededByOpenBrace_MidFile()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("Hello\u0085World");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(initialSnapshot.Content));
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(5, 1, "\u0085", syntaxTree, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotFollowedByCloseBrace()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ " + Environment.NewLine + "World");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(initialSnapshot.Content));
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(3, Environment.NewLine.Length, Environment.NewLine, syntaxTree, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsTrueIfNewLineIsSurroundedByBraces()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ \n}");
            var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create(initialSnapshot.Content));
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(3, 1, "\n", syntaxTree, documentTracker, out var context);

            // Assert
            Assert.NotNull(context);
            Assert.Same(focusedTextView, context.FocusedTextView);
            Assert.Equal(3, context.ChangePosition);
            Assert.True(result);
        }

        private static RazorSyntaxTree GetSyntaxTree(string content)
        {
            var syntaxTree = RazorSyntaxTree.Parse(
                TestRazorSourceDocument.Create(content),
                RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(FunctionsDirective.Directive);
                    options.Directives.Add(SectionDirective.Directive);
                }));

            return syntaxTree;
        }

        private static SyntaxNode ExtractSpan(int spanLocation, string content)
        {
            var syntaxTree = GetSyntaxTree(content);
            var span = syntaxTree.Root.LocateOwner(new SourceChange(new SourceSpan(spanLocation, 0), string.Empty));
            return span;
        }

        protected class TestTextContentChangedEventArgs : TextContentChangedEventArgs
        {
            public TestTextContentChangedEventArgs(INormalizedTextChangeCollection changeCollection)
                : base(CreateBeforeSnapshot(changeCollection), new Mock<ITextSnapshot>().Object, EditOptions.DefaultMinimalChange, null)
            {
            }

            protected static ITextSnapshot CreateBeforeSnapshot(INormalizedTextChangeCollection collection)
            {
                var version = new Mock<ITextVersion>();
                version.Setup(v => v.Changes)
                    .Returns(collection);
                var snapshot = new Mock<ITextSnapshot>();
                snapshot.Setup(obj => obj.Version)
                    .Returns(version.Object);

                return snapshot.Object;
            }
        }

        protected class TestTextChangeCollection : List<ITextChange>, INormalizedTextChangeCollection
        {
            public bool IncludesLineChanges => throw new NotImplementedException();
        }
    }
}
