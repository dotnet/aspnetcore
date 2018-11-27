// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class BraceSmartIndenterIntegrationTest : BraceSmartIndenterTestBase
    {
        [ForegroundFact]
        public void TextBuffer_OnPostChanged_IndentsInbetweenBraces_BaseIndentation()
        {
            // Arrange
            var change = Environment.NewLine;
            var initialSnapshot = new StringTextSnapshot("@{ }");
            var afterChangeSnapshot = new StringTextSnapshot("@{ " + change + "}");
            var edit = new TestEdit(3, 0, initialSnapshot, change.Length, afterChangeSnapshot, change);
            var expectedIndentResult = "@{ " + change + change + "}";

            var caret = CreateCaretFrom(3 + change.Length, afterChangeSnapshot);
            TestTextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer, caret);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory);

            // Act
            textBuffer.ApplyEdit(edit);

            // Assert
            Assert.Equal(expectedIndentResult, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }

        [ForegroundFact]
        public void TextBuffer_OnPostChanged_IndentsInbetweenBraces_OneLevelOfIndentation()
        {
            // Arrange
            var change = "\r";
            var initialSnapshot = new StringTextSnapshot("    @{ }");
            var afterChangeSnapshot = new StringTextSnapshot("    @{ " + change + "}");
            var edit = new TestEdit(7, 0, initialSnapshot, change.Length, afterChangeSnapshot, change);
            var expectedIndentResult = "    @{ " + change + change + "    }";

            var caret = CreateCaretFrom(7 + change.Length, afterChangeSnapshot);
            TestTextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer, caret);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory);

            // Act
            textBuffer.ApplyEdit(edit);

            // Assert
            Assert.Equal(expectedIndentResult, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }

        [ForegroundFact]
        public void TextBuffer_OnPostChanged_IndentsInbetweenDirectiveBlockBraces()
        {
            // Arrange
            var change = Environment.NewLine;
            var initialSnapshot = new StringTextSnapshot("    @functions {}");
            var afterChangeSnapshot = new StringTextSnapshot("    @functions {" + change + "}");
            var edit = new TestEdit(16, 0, initialSnapshot, change.Length, afterChangeSnapshot, change);
            var expectedIndentResult = "    @functions {" + change + change + "    }";

            var caret = CreateCaretFrom(16 + change.Length, afterChangeSnapshot);
            TestTextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer, caret);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, editorOperationsFactory);

            // Act
            textBuffer.ApplyEdit(edit);

            // Assert
            Assert.Equal(expectedIndentResult, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }
    }
}
