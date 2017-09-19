// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(IWpfTextViewConnectionListener))]
    [Export(typeof(VisualStudioDocumentTrackerFactory))]
    internal class DefaultVisualStudioDocumentTrackerFactory : VisualStudioDocumentTrackerFactory, IWpfTextViewConnectionListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly TextBufferProjectService _projectService;
        private readonly Workspace _workspace;

        [ImportingConstructor]
        public DefaultVisualStudioDocumentTrackerFactory(
            TextBufferProjectService projectService,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            
            _projectService = projectService;
            _workspace = workspace;

            _foregroundDispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
            _projectManager = workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
        }

        // This is only for testing. We want to avoid using the actual Roslyn GetService methods in unit tests.
        internal DefaultVisualStudioDocumentTrackerFactory(
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManager projectManager,
            TextBufferProjectService projectService,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectManager = projectManager;
            _projectService = projectService;
            _workspace = workspace;
        }

        public Workspace Workspace => _workspace;

        public override VisualStudioDocumentTracker GetTracker(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            _foregroundDispatcher.AssertForegroundThread();

            // While it's definitely possible to have multiple Razor text buffers attached to the same text view, there's
            // no real scenario for it. This method always returns the tracker for the first Razor text buffer, but the
            // other functionality for this class will maintain state correctly for the other buffers.
            var textBuffer = textView.BufferGraph.GetRazorBuffers().FirstOrDefault();
            if (textBuffer == null)
            {
                // No Razor buffer, nothing to track.
                return null;
            }

            // A little bit of hardening here, to make sure our assumptions are correct.
            DefaultVisualStudioDocumentTracker tracker;
            if (!textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out tracker))
            {
                Debug.Fail("The document tracker should be initialized");
            }

            Debug.Assert(tracker.TextViewsInternal.Contains(textView));
            return tracker;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            for (var i = 0; i < subjectBuffers.Count; i++)
            {
                var textBuffer = subjectBuffers[i];
                if (!textBuffer.IsRazorBuffer())
                {
                    continue;
                }

                DefaultVisualStudioDocumentTracker tracker;
                if (!textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out tracker))
                {
                    tracker = new DefaultVisualStudioDocumentTracker(_projectManager, _projectService, _workspace, textBuffer);
                    textBuffer.Properties.AddProperty(typeof(VisualStudioDocumentTracker), tracker);
                }

                if (!tracker.TextViewsInternal.Contains(textView))
                {
                    tracker.TextViewsInternal.Add(textView);
                    if (tracker.TextViewsInternal.Count == 1)
                    {
                        tracker.Subscribe();
                    }
                }
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            // This means a Razor buffer has be detached from this ITextView or the ITextView is closing. Since we keep a 
            // list of all of the open text views for each text buffer, we need to update the tracker.
            //
            // Notice that this method is called *after* changes are applied to the text buffer(s). We need to check every
            // one of them for a tracker because the content type could have changed.
            for (var i = 0; i < subjectBuffers.Count; i++)
            {
                var textBuffer = subjectBuffers[i];

                DefaultVisualStudioDocumentTracker tracker;
                if (textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out tracker))
                {
                    tracker.TextViewsInternal.Remove(textView);
                    if (tracker.TextViewsInternal.Count == 0)
                    {
                        tracker.Unsubscribe();
                    }
                }
            }
        }
    }
}
