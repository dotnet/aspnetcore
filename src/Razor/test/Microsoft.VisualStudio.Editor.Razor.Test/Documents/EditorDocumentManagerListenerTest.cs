// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    public class EditorDocumentManagerListenerTest
    {
        public EditorDocumentManagerListenerTest()
        {
            ProjectFilePath = "C:\\project1\\project.csproj";
            DocumentFilePath = "c:\\project1\\file1.cshtml";
            TextLoader = TextLoader.From(TextAndVersion.Create(SourceText.From("FILE"), VersionStamp.Default));
            FileChangeTracker = new DefaultFileChangeTracker(DocumentFilePath);

            TextBuffer = new TestTextBuffer(new StringTextSnapshot("Hello"));
        }

        private string ProjectFilePath { get; }

        private string DocumentFilePath { get; }

        private TextLoader TextLoader { get; }

        private FileChangeTracker FileChangeTracker { get; }

        private TestTextBuffer TextBuffer { get; }

        [Fact]
        public void ProjectManager_Changed_DocumentAdded_InvokesGetOrCreateDocument()
        {
            // Arrange
            var changedOnDisk = new EventHandler((o, args) => { });
            var changedInEditor = new EventHandler((o, args) => { });
            var opened = new EventHandler((o, args) => { });
            var closed = new EventHandler((o, args) => { });

            var editorDocumentManger = new Mock<EditorDocumentManager>(MockBehavior.Strict);
            editorDocumentManger
                .Setup(e => e.GetOrCreateDocument(It.IsAny<DocumentKey>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>()))
                .Returns(GetEditorDocument())
                .Callback<DocumentKey, EventHandler, EventHandler, EventHandler, EventHandler>((key, onChangedOnDisk, onChangedInEditor, onOpened, onClosed) =>
                {
                    Assert.Same(changedOnDisk, onChangedOnDisk);
                    Assert.Same(changedInEditor, onChangedInEditor);
                    Assert.Same(opened, onOpened);
                    Assert.Same(closed, onClosed);
                });

            var listener = new EditorDocumentManagerListener(editorDocumentManger.Object, changedOnDisk, changedInEditor, opened, closed);

            // Act & Assert
            listener.ProjectManager_Changed(null, new ProjectChangeEventArgs("/Path/to/project.csproj", ProjectChangeKind.DocumentAdded));
        }

        [Fact]
        public void ProjectManager_Changed_OpenDocumentAdded_InvokesOnOpened()
        {
            // Arrange
            var called = false;
            var opened = new EventHandler((o, args) => { called = true; });

            var editorDocumentManger = new Mock<EditorDocumentManager>(MockBehavior.Strict);
            editorDocumentManger
                .Setup(e => e.GetOrCreateDocument(It.IsAny<DocumentKey>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>(), It.IsAny<EventHandler>()))
                .Returns(GetEditorDocument(isOpen: true));

            var listener = new EditorDocumentManagerListener(editorDocumentManger.Object, onChangedOnDisk: null, onChangedInEditor: null, onOpened: opened, onClosed: null);

            // Act
            listener.ProjectManager_Changed(null, new ProjectChangeEventArgs("/Path/to/project.csproj", ProjectChangeKind.DocumentAdded));

            // Assert
            Assert.True(called);
        }

        private EditorDocument GetEditorDocument(bool isOpen = false)
        {
            var document = new EditorDocument(
                Mock.Of<EditorDocumentManager>(),
                ProjectFilePath,
                DocumentFilePath,
                TextLoader,
                FileChangeTracker,
                isOpen ? TextBuffer : null,
                changedOnDisk: null,
                changedInEditor: null,
                opened: null,
                closed: null);

            return document;
        }
    }
}
