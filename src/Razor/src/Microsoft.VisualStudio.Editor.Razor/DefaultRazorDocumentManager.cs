// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorDocumentManager))]
    internal class DefaultRazorDocumentManager : RazorDocumentManager
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorEditorFactoryService _editorFactoryService;
        private readonly TextBufferProjectService _projectService;

        [ImportingConstructor]
        public DefaultRazorDocumentManager(
            ForegroundDispatcher dispatcher,
            RazorEditorFactoryService editorFactoryService,
            TextBufferProjectService projectService)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (editorFactoryService == null)
            {
                throw new ArgumentNullException(nameof(editorFactoryService));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = dispatcher;
            _editorFactoryService = editorFactoryService;
            _projectService = projectService;
        }

        public override void OnTextViewOpened(ITextView textView, IEnumerable<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            foreach (var textBuffer in subjectBuffers)
            {
                if (!textBuffer.IsRazorBuffer())
                {
                    continue;
                }

                if (!IsSupportedProject(textBuffer))
                {
                    return;
                }

                if (!_editorFactoryService.TryGetDocumentTracker(textBuffer, out var documentTracker) ||
                    !(documentTracker is DefaultVisualStudioDocumentTracker tracker))
                {
                    Debug.Fail("Tracker should always be available given our expectations of the VS workflow.");
                    return;
                }

                tracker.AddTextView(textView);

                if (documentTracker.TextViews.Count == 1)
                {
                    tracker.Subscribe();
                }
            }
        }

        public override void OnTextViewClosed(ITextView textView, IEnumerable<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
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
            foreach (var textBuffer in subjectBuffers)
            {
                DefaultVisualStudioDocumentTracker documentTracker;
                if (textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out documentTracker))
                {
                    documentTracker.RemoveTextView(textView);

                    if (documentTracker.TextViews.Count == 0)
                    {
                        documentTracker.Unsubscribe();
                    }
                }
            }
        }

        private bool IsSupportedProject(ITextBuffer textBuffer)
        {
            // Fundamentally we have a Razor half of the world as soon as the document is open - and then later 
            // the C# half of the world will be initialized. This code is in general pretty tolerant of 
            // unexpected /impossible states.
            //
            // We also want to successfully shut down if the buffer is something other than .cshtml.
            object project = null;
            var isSupportedProject = false;

            // We expect the document to have a hierarchy even if it's not a real 'project'.
            // However the hierarchy can be null when the document is in the process of closing.
            if ((project = _projectService.GetHostProject(textBuffer)) != null)
            {
                isSupportedProject = _projectService.IsSupportedProject(project);
            }

            return isSupportedProject;
        }
    }
}
