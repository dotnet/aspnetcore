// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal class DefaultTextViewRazorDocumentTracker : RazorDocumentTracker
    {
        private readonly DefaultTextViewRazorDocumentTrackerService _service;
        private readonly ITextView _textView;

        private DocumentId _documentId;
        private ITextBuffer _razorBuffer;
        private bool _isSupportedProject;
        private ITextBuffer _cSharpBuffer;
        private SourceTextContainer _textContainer;
        private Workspace _workspace;
        private WorkspaceRegistration _workspaceRegistration;

        public override event EventHandler ContextChanged;

        public DefaultTextViewRazorDocumentTracker(DefaultTextViewRazorDocumentTrackerService service, ITextView textView)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            _service = service;
            _textView = textView;

            Update();

            _textView.Closed += TextView_Closed;

            _textView.BufferGraph.GraphBufferContentTypeChanged += BufferGraph_GraphBufferContentTypeChanged;
            _textView.BufferGraph.GraphBuffersChanged += BufferGraph_GraphBuffersChanged;
        }

        public override bool IsSupportedDocument => _isSupportedProject && _textContainer != null;

        public override ProjectId ProjectId => _documentId?.ProjectId;

        public override SourceTextContainer TextContainer => _textContainer;

        public override Workspace Workspace => _workspace;

        private bool Update()
        {
            // Update is called when the state of any of our surrounding systems changes. Here we want to examine the
            // state of the world and then update properties as necessary.
            //
            // Fundamentally we have a Razor half of the world as as soon as the document is open - and then later 
            // the C# half of the world will be initialized. This code is in general pretty tolerant of 
            // unexpected /impossible states.
            //
            // We also want to successfully shut down when the buffer is renamed to something other .cshtml.

            // If there's no Razor buffer, then there's really nothing to do. This isn't a Razor file.
            var razorBuffer = _textView.BufferGraph.GetTextBuffers(IsRazorBuffer).FirstOrDefault();

            // The source container is tied to the Razor buffer since that's what we want to watch for changes.
            SourceTextContainer textContainer = null;
            if (razorBuffer != null)
            {
                textContainer = razorBuffer.AsTextContainer();
            }

            bool isSupportedProject = false;
            if (razorBuffer != null)
            {
                // The project can be null when the document is in the process of closing.
                var project = _service.GetProject(razorBuffer);

                if (project != null)
                {
                    isSupportedProject = _service.IsSupportedProject(project);
                }
            }

            // The C# buffer is the other buffer in the graph that is C# (only if we already found a Razor buffer).
            // We're not currently validating that it's a child of the Razor buffer even though we expect that.
            ITextBuffer cSharpBuffer = null;
            if (razorBuffer != null)
            {
                cSharpBuffer = _textView.BufferGraph.GetTextBuffers(IsCSharpBuffer).FirstOrDefault();
            }

            // Now if we have a C# buffer we want to watch for it be attached to a workspace.
            SourceTextContainer cSharpTextContainer = null;
            WorkspaceRegistration workspaceRegistration = null;
            if (cSharpBuffer != null)
            {
                cSharpTextContainer = cSharpBuffer.AsTextContainer();
                Debug.Assert(cSharpTextContainer != null);

                workspaceRegistration = Workspace.GetWorkspaceRegistration(cSharpTextContainer);
            }

            // Now finally we can see if we have a workspace
            Workspace workspace = null;
            if (workspaceRegistration != null)
            {
                workspace = workspaceRegistration.Workspace;
            }

            // Now we know if the Roslyn state for this document has been initialized, let's look for a project.
            DocumentId documentId = null;
            if (cSharpTextContainer != null && workspace != null)
            {
                documentId = workspace.GetDocumentIdInCurrentContext(cSharpTextContainer);
            }

            // As a special case, we want to default to the VisualStudioWorkspace until we find out otherwise
            // This lets us start working before a project gets initialized.
            if (isSupportedProject && workspace == null)
            {
                workspace = _service.Workspace;
            }
            
            var changed = false;
            changed |= razorBuffer == _razorBuffer;
            changed |= textContainer == _textContainer;
            changed |= isSupportedProject == _isSupportedProject;
            changed |= cSharpBuffer == _cSharpBuffer;
            changed |= workspaceRegistration == _workspaceRegistration;
            changed |= workspace == _workspace;
            changed |= documentId == _documentId;

            // Now if nothing has changed we're all done!
            if (!changed)
            {
                return false;
            }

            // OK, so something did change, let's commit the changes.
            //
            // These are all of the straightforward ones.
            _razorBuffer = razorBuffer;
            _textContainer = textContainer;
            _isSupportedProject = isSupportedProject;
            _cSharpBuffer = cSharpBuffer;
            _documentId = documentId;

            // Now these ones we subscribe to events so it's a little tricky.
            if (_workspaceRegistration != null)
            {
                _workspaceRegistration.WorkspaceChanged -= WorkspaceRegistration_WorkspaceChanged;
            }
            if (workspaceRegistration != null)
            {
                workspaceRegistration.WorkspaceChanged += WorkspaceRegistration_WorkspaceChanged;
            }

            if (_workspace != null)
            {
                _workspace.DocumentActiveContextChanged -= Workspace_DocumentActiveContextChanged;
            }
            if (workspace != null)
            {
                workspace.DocumentActiveContextChanged += Workspace_DocumentActiveContextChanged;
            }

            _workspaceRegistration = workspaceRegistration;
            _workspace = workspace;

            return true;
        }

        private static bool IsCSharpBuffer(ITextBuffer textBuffer)
        {
            return textBuffer.ContentType.IsOfType("CSharp");
        }

        private static bool IsRazorBuffer(ITextBuffer textBuffer)
        {
            return textBuffer.ContentType.IsOfType(RazorLanguage.ContentType);
        }

        private void BufferGraph_GraphBuffersChanged(object sender, GraphBuffersChangedEventArgs e)
        {
            if (Update())
            {
                OnContextChanged();
            }
        }

        private void BufferGraph_GraphBufferContentTypeChanged(object sender, GraphBufferContentTypeChangedEventArgs e)
        {
            if (Update())
            {
                OnContextChanged();
            }
        }

        private void WorkspaceRegistration_WorkspaceChanged(object sender, EventArgs e)
        {
            if (Update())
            {
                OnContextChanged();
            }
        }

        private void Workspace_DocumentActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs e)
        {
            var textBuffer = e.SourceTextContainer.GetTextBuffer();
            if (textBuffer != null && (textBuffer == _cSharpBuffer || textBuffer == _razorBuffer))
            {
                if (Update())
                {
                    OnContextChanged();
                }
            }
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            _textView.BufferGraph.GraphBufferContentTypeChanged -= BufferGraph_GraphBufferContentTypeChanged;
            _textView.BufferGraph.GraphBuffersChanged -= BufferGraph_GraphBuffersChanged;

            if (_workspaceRegistration != null)
            {
                _workspaceRegistration.WorkspaceChanged -= WorkspaceRegistration_WorkspaceChanged;
            }

            if (_workspace != null)
            {
                _workspace.DocumentActiveContextChanged -= Workspace_DocumentActiveContextChanged;
            }

            _textView.Closed -= TextView_Closed;

            _razorBuffer = null;
            _textContainer = null;
            _isSupportedProject = false;
            _cSharpBuffer = null;
            _workspaceRegistration = null;
            _workspace = null;
            _documentId = null;

            OnContextChanged();
        }

        private void OnContextChanged()
        {
            var handler = ContextChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
