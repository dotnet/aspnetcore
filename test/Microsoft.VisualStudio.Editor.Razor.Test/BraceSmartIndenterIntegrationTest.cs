// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
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
            var codeDocumentProvider = CreateCodeDocumentProvider(initialSnapshot.Content);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory);

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
            var codeDocumentProvider = CreateCodeDocumentProvider(initialSnapshot.Content);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory);

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
            var codeDocumentProvider = CreateCodeDocumentProvider(initialSnapshot.Content);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory);

            // Act
            textBuffer.ApplyEdit(edit);

            // Assert
            Assert.Equal(expectedIndentResult, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }

        [ForegroundFact]
        public void TextBuffer_OnPostChanged_DoesNotIndentJavaScript()
        {
            // Arrange
            var change = Environment.NewLine;
            var initialSnapshot = new StringTextSnapshot("    <script>function foo() {}</script>");
            var afterChangeSnapshot = new StringTextSnapshot("    <script>function foo() {" + change + "}</script>");
            var edit = new TestEdit(28, 0, initialSnapshot, change.Length, afterChangeSnapshot, change);

            var caret = CreateCaretFrom(28 + change.Length, afterChangeSnapshot);
            TestTextBuffer textBuffer = null;
            var focusedTextView = CreateFocusedTextView(() => textBuffer, caret);
            var documentTracker = CreateDocumentTracker(() => textBuffer, focusedTextView);
            textBuffer = CreateTextBuffer(initialSnapshot, documentTracker);
            var codeDocumentProvider = CreateCodeDocumentProvider(initialSnapshot.Content);
            var editorOperationsFactory = CreateOperationsFactoryService();
            var braceSmartIndenter = new BraceSmartIndenter(Dispatcher, documentTracker, codeDocumentProvider, editorOperationsFactory);

            // Act
            textBuffer.ApplyEdit(edit);

            // Assert
            Assert.Equal(afterChangeSnapshot.Content, ((StringTextSnapshot)textBuffer.CurrentSnapshot).Content);
        }

        private TextBufferCodeDocumentProvider CreateCodeDocumentProvider(string content)
        {
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(opt => opt.Directives.Add(FunctionsDirective.Directive)));
            var codeDocument = TestRazorCodeDocument.Create(content);
            codeDocument.SetSyntaxTree(syntaxTree);
            var codeDocumentProvider = Mock.Of<TextBufferCodeDocumentProvider>(provider => provider.TryGetFromBuffer(It.IsAny<ITextBuffer>(), out codeDocument));

            return codeDocumentProvider;
        }
    }
}
