// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    public class EditorDocumentTest
    {
        public EditorDocumentTest()
        {
            DocumentManager = Mock.Of<EditorDocumentManager>();
            ProjectFilePath = "C:\\project1\\project.csproj";
            DocumentFilePath = "c:\\project1\\file1.cshtml";
            TextLoader = TextLoader.From(TextAndVersion.Create(SourceText.From("FILE"), VersionStamp.Default));
            FileChangeTracker = new DefaultFileChangeTracker(DocumentFilePath);

            TextBuffer = new TestTextBuffer(new StringTextSnapshot("Hello"));
        }

        private EditorDocumentManager DocumentManager { get; }

        private string ProjectFilePath { get; }

        private string DocumentFilePath { get; }

        private TextLoader TextLoader { get; }

        private FileChangeTracker FileChangeTracker { get; }

        private TestTextBuffer TextBuffer { get; }

        [Fact]
        public void EditorDocument_CreatedWhileOpened()
        {
            // Arrange & Act
            var document = new EditorDocument(
                DocumentManager,
                ProjectFilePath,
                DocumentFilePath,
                TextLoader,
                FileChangeTracker,
                TextBuffer,
                changedOnDisk: null,
                changedInEditor: null,
                opened: null,
                closed: null);

            // Assert
            Assert.True(document.IsOpenInEditor);
            Assert.Same(TextBuffer, document.EditorTextBuffer);
            Assert.NotNull(document.EditorTextContainer);
        }

        [Fact]
        public void EditorDocument_CreatedWhileClosed()
        {
            // Arrange & Act
            var document = new EditorDocument(
                DocumentManager,
                ProjectFilePath,
                DocumentFilePath,
                TextLoader,
                FileChangeTracker,
                null,
                changedOnDisk: null,
                changedInEditor: null,
                opened: null,
                closed: null);
            
            // Assert
            Assert.False(document.IsOpenInEditor);
            Assert.Null(document.EditorTextBuffer);
            Assert.Null(document.EditorTextContainer);
        }

        private class TestSourceTextContainer : SourceTextContainer
        {
            public override event EventHandler<TextChangeEventArgs> TextChanged;

            private SourceText _currentText;

            public TestSourceTextContainer()
                : this(SourceText.From(string.Empty))
            {
            }

            public TestSourceTextContainer(SourceText text)
            {
                _currentText = text;
            }

            public override SourceText CurrentText => _currentText;

            public void PushChange(SourceText text)
            {
                var args = new TextChangeEventArgs(_currentText, text);
                _currentText = text;

                TextChanged?.Invoke(this, args);
            }
        }
    }
}
