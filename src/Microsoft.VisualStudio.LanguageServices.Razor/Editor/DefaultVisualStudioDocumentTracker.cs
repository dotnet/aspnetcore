// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    internal class DefaultVisualStudioDocumentTracker : VisualStudioDocumentTracker
    {
        private readonly TextBufferProjectService _projectService;
        private readonly ITextBuffer _textBuffer;
        private readonly List<ITextView> _textViews;
        
        private bool _isSupportedProject;
        private Workspace _workspace;

        public override event EventHandler ContextChanged;

        public DefaultVisualStudioDocumentTracker(TextBufferProjectService projectService, Workspace workspace, ITextBuffer textBuffer)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _projectService = projectService;
            _textBuffer = textBuffer;
            _workspace = workspace;

            _textViews = new List<ITextView>();
            
            Update();
        }

        public override bool IsSupportedProject => _isSupportedProject;

        public override ProjectId ProjectId => null;

        public override ITextBuffer TextBuffer => _textBuffer;

        public override IReadOnlyList<ITextView> TextViews => _textViews;

        public IList<ITextView> TextViewsInternal => _textViews;

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
            IVsHierarchy project = null;
            var isSupportedProject = false;
            if (_textBuffer.ContentType.IsOfType(RazorLanguage.ContentType) &&
                (project = _projectService.GetHierarchy(_textBuffer)) != null)
            {
                // We expect the document to have a hierarchy even if it's not a real 'project'.
                // However the hierarchy can be null when the document is in the process of closing.
                isSupportedProject = _projectService.IsSupportedProject(project);
            }

            // For now we temporarily assume that the workspace is the default VS workspace.
            var workspace = _workspace;
            
            var changed = false;
            changed |= isSupportedProject == _isSupportedProject;
            changed |= workspace == _workspace;

            if (changed)
            {
                _isSupportedProject = isSupportedProject;
                _workspace = workspace;
            }

            return changed;
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
