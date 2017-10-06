// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class DefaultVisualStudioDocumentTrackerFactoryTest : ForegroundDispatcherTestBase
    {
        private static IReadOnlyList<ProjectSnapshot> Projects = new List<ProjectSnapshot>();

        private ProjectSnapshotManager ProjectManager { get; } = Mock.Of<ProjectSnapshotManager>(p => p.Projects == Projects);

        private TextBufferProjectService ProjectService { get; } = Mock.Of<TextBufferProjectService>(
            s => s.GetHierarchy(It.IsAny<ITextBuffer>()) == Mock.Of<IVsHierarchy>() &&
            s.IsSupportedProject(It.IsAny<IVsHierarchy>()) == true);

        private Workspace Workspace { get; } = new AdhocWorkspace();

        private IContentType RazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);

        private IContentType NonRazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);

        [ForegroundFact]
        public void SubjectBuffersConnected_ForNonRazorTextBuffer_DoesNothing()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            factory.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.False(buffers[0].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));
        }

        [ForegroundFact]
        public void SubjectBuffersConnected_ForRazorTextBufferWithoutTracker_CreatesTrackerAndTracksTextView()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            factory.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            var tracker = buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView));
            Assert.Equal(buffers[0], tracker.TextBuffer);
        }

        [ForegroundFact]
        public void SubjectBuffersConnected_ForRazorTextBufferWithoutTracker_CreatesTrackerAndTracksTextView_ForMultipleBuffers()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            factory.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            var tracker = buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView));
            Assert.Equal(buffers[0], tracker.TextBuffer);

            Assert.False(buffers[1].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));

            tracker = buffers[2].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView));
            Assert.Equal(buffers[2], tracker.TextBuffer);
        }

        [ForegroundFact]
        public void SubjectBuffersConnected_ForRazorTextBufferWithTracker_DoesNotAddDuplicateTextViewEntry()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, buffers[0]);
            tracker.TextViewsInternal.Add(textView);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            // Act
            factory.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.Same(tracker, buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker)));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView));
        }

        [ForegroundFact]
        public void SubjectBuffersConnected_ForRazorTextBufferWithTracker_AddsEntryForADifferentTextView()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView1 = Mock.Of<IWpfTextView>();
            var textView2 = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, buffers[0]);
            tracker.TextViewsInternal.Add(textView1);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            // Act
            factory.SubjectBuffersConnected(textView2, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.Same(tracker, buffers[0].Properties.GetProperty<DefaultVisualStudioDocumentTracker>(typeof(VisualStudioDocumentTracker)));
            Assert.Collection(tracker.TextViews, v => Assert.Same(v, textView1), v => Assert.Same(v, textView2));
        }

        [ForegroundFact]
        public void SubjectBuffersDisconnected_ForAnyTextBufferWithTracker_RemovesTextView()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView1 = Mock.Of<IWpfTextView>();
            var textView2 = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
                Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection()),
            };

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, buffers[0]);
            tracker.TextViewsInternal.Add(textView1);
            tracker.TextViewsInternal.Add(textView2);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, buffers[1]);
            tracker.TextViewsInternal.Add(textView1);
            tracker.TextViewsInternal.Add(textView2);
            buffers[1].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            // Act
            factory.SubjectBuffersDisconnected(textView2, ConnectionReason.BufferGraphChange, buffers);

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
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var textView = Mock.Of<IWpfTextView>();

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            // Act
            factory.SubjectBuffersDisconnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            Assert.False(buffers[0].Properties.ContainsProperty(typeof(VisualStudioDocumentTracker)));
        }

        [ForegroundFact]
        public void GetTracker_ITextBuffer_ForRazorTextBufferWithTracker_ReturnsTracker()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection());

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, textBuffer);
            textBuffer.Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            // Act
            var result = factory.GetTracker(textBuffer);

            // Assert
            Assert.Same(tracker, result);
        }

        [ForegroundFact]
        public void GetTracker_ITextBuffer_NonRazorBuffer_ReturnsNull()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factory.GetTracker(textBuffer);

            // Assert
            Assert.Null(result);
        }

        [ForegroundFact]
        public void GetTracker_ITextView_ForRazorTextBufferWithTracker_ReturnsTheFirstTracker()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var buffers = new Collection<ITextBuffer>()
            {
                Mock.Of<ITextBuffer>(b => b.ContentType == RazorContentType && b.Properties == new PropertyCollection()),
            };

            var bufferGraph = Mock.Of<IBufferGraph>(g => g.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()) == buffers);

            var textView = Mock.Of<IWpfTextView>(v => v.BufferGraph == bufferGraph);

            // Preload the buffer's properties with a tracker, so it's like we've already tracked this one.
            var tracker = new DefaultVisualStudioDocumentTracker(ProjectManager, ProjectService, Workspace, buffers[0]);
            tracker.TextViewsInternal.Add(textView);
            buffers[0].Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);

            // Act
            var result = factory.GetTracker(textView);

            // Assert
            Assert.Same(tracker, result);
        }

        [ForegroundFact]
        public void GetTracker_ITextView_WithoutRazorBuffer_ReturnsNull()
        {
            // Arrange
            var factory = new DefaultVisualStudioDocumentTrackerFactory(Dispatcher, ProjectManager, ProjectService, Workspace);

            var buffers = new Collection<ITextBuffer>();

            var bufferGraph = Mock.Of<IBufferGraph>(g => g.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()) == buffers);

            var textView = Mock.Of<IWpfTextView>(v => v.BufferGraph == bufferGraph);

            // Act
            var result = factory.GetTracker(textView);

            // Assert
            Assert.Null(result);
        }
    }
}
