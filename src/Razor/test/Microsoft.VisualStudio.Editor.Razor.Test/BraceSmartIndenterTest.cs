// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class BraceSmartIndenterTest : BraceSmartIndenterTestBase
    {
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
            var smartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory.Object);

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
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory.Object);

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
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory.Object);

            // Act & Assert
            textBuffer.ApplyEdits(edit, edit);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNoFocusedTextView()
        {
            // Arrange
            var snapshot = new StringTextSnapshot(Environment.NewLine + "Hello World");
            ITextBuffer textBuffer = null;
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView: null);
            textBuffer = CreateTextBuffer(snapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, Environment.NewLine.Length, Environment.NewLine, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfTextChangeIsNotNewline()
        {
            // Arrange
            var snapshot = new StringTextSnapshot("This Hello World");
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(snapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, 5, "This ", documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotPrecededByOpenBrace_FileStart()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot(Environment.NewLine + "Hello World");
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(0, Environment.NewLine.Length, Environment.NewLine, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotPrecededByOpenBrace_MidFile()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("Hello\u0085World");
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(5, 1, "\u0085", documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsFalseIfNewLineIsNotFollowedByCloseBrace()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ " + Environment.NewLine + "World");
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(3, Environment.NewLine.Length, Environment.NewLine, documentTracker, out var context);

            // Assert
            Assert.Null(context);
            Assert.False(result);
        }

        [Fact]
        public void TryCreateIndentationContext_ReturnsTrueIfNewLineIsSurroundedByBraces()
        {
            // Arrange
            var initialSnapshot = new StringTextSnapshot("@{ \n}");
            ITextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);

            // Act
            var result = BraceSmartIndenter.TryCreateIndentationContext(3, 1, "\n", documentTracker, out var context);

            // Assert
            Assert.NotNull(context);
            Assert.Same(focusedTextView, context.FocusedTextView);
            Assert.Equal(3, context.ChangePosition);
            Assert.True(result);
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
