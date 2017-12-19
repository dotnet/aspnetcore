// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class DefaultRazorDocumentManagerTest : ForegroundDispatcherTestBase
    {
        private IContentType RazorCoreContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.CoreContentType) == true);

        private IContentType NonRazorCoreContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);

        private string FilePath => "C:/Some/Path/TestDocumentTracker.cshtml";

        private string ProjectPath => "C:/Some/Path/TestProject.csproj";

        private ProjectSnapshotManager ProjectManager => Mock.Of<ProjectSnapshotManager>(p => p.Projects == new List<ProjectSnapshot>());

        private EditorSettingsManagerInternal EditorSettingsManager => new DefaultEditorSettingsManagerInternal(Dispatcher);

        private ImportDocumentManager ImportDocumentManager => Mock.Of<ImportDocumentManager>();

        private Workspace Workspace => new AdhocWorkspace();

        private TextBufferProjectService SupportedProjectService { get; } = Mock.Of<TextBufferProjectService>(
            s => s.GetHostProject(It.IsAny<ITextBuffer>()) == Mock.Of<object>() &&
            s.IsSupportedProject(It.IsAny<object>()) == true &&
                s.GetProjectPath(It.IsAny<object>()) == "C:/Some/Path/TestProject.csproj");

        private TextBufferProjectService UnsupportedProjectService { get; } = Mock.Of<TextBufferProjectService>(s => s.IsSupportedProject(It.IsAny<object>()) == false);

        [ForegroundFact]
        public void OnTextViewOpened_ForNonRazorCoreProject_DoesNothing()
        {
            // Arrange
            var editorFactoryService = new Mock<RazorEditorFactoryService>(MockBehavior.Strict);
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService.Object, UnsupportedProjectService);
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
            };

            // Act & Assert
            documentManager.OnTextViewOpened(textView, buffers);
        }

        [ForegroundFact]
        public void OnTextViewOpened_ForNonRazorTextBuffer_DoesNothing()
        {
            // Arrange
            var editorFactoryService = new Mock<RazorEditorFactoryService>(MockBehavior.Strict);
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService.Object, SupportedProjectService);
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection()),
            };

            // Act & Assert
            documentManager.OnTextViewOpened(textView, buffers);
        }

        [ForegroundFact]
        public void OnTextViewOpened_ForRazorTextBuffer_AddsTextViewToTracker()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
            };
            var documentTracker = new DefaultVisualStudioDocumentTracker(Dispatcher, FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, buffers[0], ImportDocumentManager) as VisualStudioDocumentTracker;
            var editorFactoryService = Mock.Of<RazorEditorFactoryService>(factoryService => factoryService.TryGetDocumentTracker(It.IsAny<ITextBuffer>(), out documentTracker) == true);
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService, SupportedProjectService);

            // Act
            documentManager.OnTextViewOpened(textView, buffers);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [ForegroundFact]
        public void OnTextViewOpened_SubscribesAfterFirstTextViewOpened()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection()),
            };
            var documentTracker = new DefaultVisualStudioDocumentTracker(Dispatcher, FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, buffers[0], ImportDocumentManager) as VisualStudioDocumentTracker;
            var editorFactoryService = Mock.Of<RazorEditorFactoryService>(f => f.TryGetDocumentTracker(It.IsAny<ITextBuffer>(), out documentTracker) == true);
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService, SupportedProjectService);

            // Assert 1
            Assert.False(documentTracker.IsSupportedProject);

            // Act
            documentManager.OnTextViewOpened(textView, buffers);

            // Assert 2
            Assert.True(documentTracker.IsSupportedProject);
        }

        [ForegroundFact]
        public void OnTextViewClosed_FoNonRazorCoreProject_DoesNothing()
        {
            // Arrange
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, Mock.Of<RazorEditorFactoryService>(), UnsupportedProjectService);
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            documentManager.OnTextViewClosed(textView, buffers);

            // Assert
            Assert.False(buffers[0].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));
        }

        [ForegroundFact]
        public void OnTextViewClosed_TextViewWithoutDocumentTracker_DoesNothing()
        {
            // Arrange
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, Mock.Of<RazorEditorFactoryService>(), SupportedProjectService);
            var textView = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            documentManager.OnTextViewClosed(textView, buffers);

            // Assert
            Assert.False(buffers[0].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));
        }

        [ForegroundFact]
        public void OnTextViewClosed_ForAnyTextBufferWithTracker_RemovesTextView()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection()),
            };

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var documentTracker = new DefaultVisualStudioDocumentTracker(Dispatcher, FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, buffers[0], ImportDocumentManager);
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), documentTracker);

            documentTracker = new DefaultVisualStudioDocumentTracker(Dispatcher, FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, buffers[1], ImportDocumentManager);
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);
            buffers[1].Properties.AddProperty(typeof(VisualStudioDocumentTracker), documentTracker);

            var editorFactoryService = Mock.Of<RazorEditorFactoryService>();
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService, SupportedProjectService);

            // Act
            documentManager.OnTextViewClosed(textView2, buffers);

            // Assert
            documentTracker = buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView1));

            documentTracker = buffers[1].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView1));
        }

        [ForegroundFact]
        public void OnTextViewClosed_UnsubscribesAfterLastTextViewClosed()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection()),
            };
            var documentTracker = new DefaultVisualStudioDocumentTracker(Dispatcher, FilePath, ProjectPath, ProjectManager, EditorSettingsManager, Workspace, buffers[0], ImportDocumentManager);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), documentTracker);
            var editorFactoryService = Mock.Of<RazorEditorFactoryService>();
            var documentManager = new DefaultRazorDocumentManager(Dispatcher, editorFactoryService, SupportedProjectService);

            // Populate the text views
            documentTracker.Subscribe();
            documentTracker.AddTextView(textView1);
            documentTracker.AddTextView(textView2);

            // Act 1
            documentManager.OnTextViewClosed(textView2, buffers);

            // Assert 1
            Assert.True(documentTracker.IsSupportedProject);

            // Act
            documentManager.OnTextViewClosed(textView1, buffers);

            // Assert 2
            Assert.False(documentTracker.IsSupportedProject);
        }
    }
}
