// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultVisualStudioDocumentTrackerTest
    {
        private IContentType RazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);

        private ITextBuffer TextBuffer => Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType);

        private string FilePath => "C:/Some/Path/TestDocumentTracker.cshtml";

        private string ProjectPath => "C:/Some/Path/TestProject.csproj";

        private ProjectSnapshotManager ProjectManager => Mock.Of<ProjectSnapshotManager>(p => p.Projects == new List<ProjectSnapshot>());

        private EditorSettingsManagerInternal EditorSettingsManager => new DefaultEditorSettingsManagerInternal();

        private Workspace Workspace => new AdhocWorkspace();

        [Fact]
        public void EditorSettingsManager_Changed_TriggersContextChanged()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
            var called = false;
            documentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
                Assert.Equal(ContextChangeKind.EditorSettingsChanged, args.Kind);
            };

            // Act
            documentTracker.EditorSettingsManager_Changed(null, null);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Subscribe_SetsSupportedProjectAndTriggersContextChanged()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
            var called = false;
            documentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
                Assert.Equal(ContextChangeKind.ProjectChanged, args.Kind);
            };

            // Act
            documentTracker.Subscribe();

            // Assert
            Assert.True(called);
            Assert.True(documentTracker.IsSupportedProject);
        }

        [Fact]
        public void Unsubscribe_ResetsSupportedProjectAndTriggersContextChanged()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);

            // Subscribe once to set supported project
            documentTracker.Subscribe();

            var called = false;
            documentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
                Assert.Equal(ContextChangeKind.ProjectChanged, args.Kind);
            };

            // Act
            documentTracker.Unsubscribe();

            // Assert
            Assert.False(documentTracker.IsSupportedProject);
            Assert.True(called);
        }

        [Fact]
        public void AddTextView_AddsToTextViewCollection()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
            var textView = Mock.Of<ITextView>();

            // Act
            documentTracker.AddTextView(textView);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [Fact]
        public void AddTextView_DoesNotAddDuplicateTextViews()
        {
            // Arrange
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
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
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
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
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
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
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
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
            var documentTracker = new DefaultVisualStudioDocumentTracker(FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, TextBuffer);
            var textView1 = Mock.Of<ITextView>();
            documentTracker.AddTextView(textView1);
            var textView2 = Mock.Of<ITextView>();

            // Act
            documentTracker.RemoveTextView(textView2);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView1));
        }
    }
}
