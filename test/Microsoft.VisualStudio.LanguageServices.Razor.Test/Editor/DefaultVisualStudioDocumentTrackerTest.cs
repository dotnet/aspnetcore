// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class DefaultVisualStudioDocumentTrackerTest
    {
        private IContentType RazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);

        private ITextBuffer TextBuffer => Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType);

        private string FilePath => "C:/Some/Path/TestDocumentTracker.cshtml";

        private ProjectSnapshotManager ProjectManager => Mock.Of<ProjectSnapshotManager>(p => p.Projects == new List<ProjectSnapshot>());

        private TextBufferProjectService ProjectService => Mock.Of<TextBufferProjectService>(
            s => s.GetHierarchy(It.IsAny<ITextBuffer>()) == Mock.Of<IVsHierarchy>() &&
                s.IsSupportedProject(It.IsAny<IVsHierarchy>()) == true &&
                s.GetProjectPath(It.IsAny<IVsHierarchy>()) == "C:/Some/Path/TestProject.csproj");

        private EditorSettingsManager EditorSettingsManager => new DefaultEditorSettingsManager();

        private Workspace Workspace => new AdhocWorkspace();

        [Fact]
        public void EditorSettingsManager_Changed_TriggersContextChanged()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var called = false;
            documentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
            };

            // Act
            documentTracker.EditorSettingsManager_Changed(null, null);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void AddTextView_AddsToTextViewCollection()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView = Mock.Of<ITextView>();

            // Act
            documentTracker.AddTextView(textView);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [Fact]
        public void AddTextView_SubscribesAfterFirstTextViewAdded()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView = Mock.Of<ITextView>();

            // Assert - 1
            Assert.False(documentTracker.IsSupportedProject);

            // Act
            documentTracker.AddTextView(textView);

            // Assert - 2
            Assert.True(documentTracker.IsSupportedProject);
        }

        [Fact]
        public void AddTextView_DoesNotAddDuplicateTextViews()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView = Mock.Of<ITextView>();

            // Act
            documentTracker.AddTextView(textView);
            documentTracker.AddTextView(textView);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [Fact]
        public void AddTextView_AddsMultipleTextViewsToCollection()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();

            // Act
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);

            // Assert
            Assert.Collection(
                documentTracker.TextViews,
                v => Assert.Same(v, textView1),
                v => Assert.Same(v, textView2));
        }

        [Fact]
        public void RemoveTextView_RemovesTextViewFromCollection_SingleItem()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView = Mock.Of<ITextView>();
            documentTracker.AddTextView(textView);

            // Act
            documentTracker.RemoveTextView(textView);

            // Assert
            Assert.Empty(documentTracker.TextViews);
        }

        [Fact]
        public void RemoveTextView_RemovesTextViewFromCollection_MultipleItems()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            var textView3 = Mock.Of<ITextView>();
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);
            documentTracker.AddTextView(textView3);

            // Act
            documentTracker.RemoveTextView(textView2);

            // Assert
            Assert.Collection(
                documentTracker.TextViews,
                v => Assert.Same(v, textView1),
                v => Assert.Same(v, textView3));
        }

        [Fact]
        public void RemoveTextView_NoopsWhenRemovingTextViewNotInCollection()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView1 = Mock.Of<ITextView>();
            documentTracker.AddTextView(textView1);
            var textView2 = Mock.Of<ITextView>();

            // Act
            documentTracker.RemoveTextView(textView2);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView1));
        }

        [Fact]
        public void RemoveTextView_UnsubscribesAfterLastTextViewRemoved()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectManager, ProjectService, EditorSettingsManager, Workspace, TextBuffer);
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);

            // Act - 1
            documentTracker.RemoveTextView(textView1);

            // Assert - 1
            Assert.True(documentTracker.IsSupportedProject);

            // Act - 2
            documentTracker.RemoveTextView(textView2);

            // Assert - 2
            Assert.False(documentTracker.IsSupportedProject);
        }
    }
}
