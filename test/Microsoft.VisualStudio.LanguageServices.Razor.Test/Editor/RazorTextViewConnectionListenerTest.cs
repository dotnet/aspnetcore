// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class RazorTextViewConnectionListenerTest : ForegroundDispatcherTestBase
    {
        private ProjectSnapshotManager ProjectManager { get; } = Mock.Of<ProjectSnapshotManager>(p => p.Projects == new List<ProjectSnapshot>());

        private TextBufferProjectService ProjectService { get; } = Mock.Of<TextBufferProjectService>(
            s => s.GetHierarchy(It.IsAny<ITextBuffer>()) == Mock.Of<IVsHierarchy>() &&
            s.IsSupportedProject(It.IsAny<IVsHierarchy>()) == true &&
                s.GetProjectPath(It.IsAny<IVsHierarchy>()) == "C:/Some/Path/TestProject.csproj");

        private EditorSettingsManager EditorSettingsManager => new DefaultEditorSettingsManager();

        private Workspace Workspace { get; } = new AdhocWorkspace();

        private IContentType RazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);

        private IContentType NonRazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);

        [ForegroundFact]
        public void SubjectBuffersConnected_ForNonRazorTextBuffer_DoesNothing()
        {
            // Arrange
            var editorFactoryService = new Mock<RazorEditorFactoryService>(MockBehavior.Strict);
            var factory = new RazorTextViewConnectionListener(Dispatcher, editorFactoryService.Object, Workspace);
            var textView = Mock.Of<IWpfTextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act & Assert
            factory.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);
        }

        [ForegroundFact]
        public void SubjectBuffersConnected_ForRazorTextBuffer_AddsTextViewToTracker()
        {
            // Arrange
            var textView = Mock.Of<IWpfTextView>();
            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };
            VisualStudioDocumentTracker documentTracker = new DefaultVisualStudioDocumentTracker("AFile", ProjectManager, ProjectService, EditorSettingsManager, Workspace, buffers[0]);
            var editorFactoryService = Mock.Of<RazorEditorFactoryService>(factoryService => factoryService.TryGetDocumentTracker(It.IsAny<ITextBuffer>(), out documentTracker) == true);
            var textViewListener = new RazorTextViewConnectionListener(Dispatcher, editorFactoryService, Workspace);

            // Act
            textViewListener.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.Collection(documentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [ForegroundFact]
        public void SubjectBuffersDisconnected_ForAnyTextBufferWithTracker_RemovesTextView()
        {
            // Arrange
            var textView1 = Mock.Of<IWpfTextView>();
            var textView2 = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection()),
            };

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker("C:/File/Path/To/Tracker1.cshtml", ProjectManager, ProjectService, EditorSettingsManager, Workspace, buffers[0]);
            tracker.AddTextView(textView1);
            tracker.AddTextView(textView2);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            tracker = new DefaultVisualStudioDocumentTracker("C:/File/Path/To/Tracker1.cshtml", ProjectManager, ProjectService, EditorSettingsManager, Workspace, buffers[1]);
            tracker.AddTextView(textView1);
            tracker.AddTextView(textView2);
            buffers[1].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);
            var textViewListener = new RazorTextViewConnectionListener(Dispatcher, Mock.Of<RazorEditorFactoryService>(), Workspace);

            // Act
            textViewListener.SubjectBuffersDisconnected(textView2, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            tracker = buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView1));

            tracker = buffers[1].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView1));
        }

        [ForegroundFact]
        public void SubjectBuffersDisconnected_ForAnyTextBufferWithoutTracker_DoesNothing()
        {
            // Arrange
            var textViewListener = new RazorTextViewConnectionListener(Dispatcher, Mock.Of<RazorEditorFactoryService>(), Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            textViewListener.SubjectBuffersDisconnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.False(buffers[0].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));
        }
    }
}
