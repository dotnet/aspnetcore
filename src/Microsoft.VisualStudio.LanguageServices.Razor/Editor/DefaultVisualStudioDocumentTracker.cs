// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    internal class DefaultVisualStudioDocumentTracker : VisualStudioDocumentTracker
    {
        private readonly ProjectSnapshotManager _projectManager;
        private readonly TextBufferProjectService _projectService;
        private readonly ITextBuffer _textBuffer;
        private readonly List<ITextView> _textViews;
        private readonly Workspace _workspace;

        private bool _isSupportedProject;
        private ProjectSnapshot _project;
        private string _projectPath;
        private ProjectSnapshotListener _subscription;

        public override event EventHandler ContextChanged;

        public DefaultVisualStudioDocumentTracker(
            ProjectSnapshotManager projectManager,
            TextBufferProjectService projectService,
            Workspace workspace,
            ITextBuffer textBuffer)
        {
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

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _projectManager = projectManager;
            _projectService = projectService;
            _textBuffer = textBuffer;
            _workspace = workspace; // For now we assume that the workspace is the always default VS workspace.

            _textViews = new List<ITextView>();

            Initialize();
        }

        public override bool IsSupportedProject => _isSupportedProject;

        public override Project Project => _project?.UnderlyingProject;

        public override ITextBuffer TextBuffer => _textBuffer;

        public override IReadOnlyList<ITextView> TextViews => _textViews;

        public IList<ITextView> TextViewsInternal => _textViews;

        public override Workspace Workspace => _workspace;

        private void Initialize()
        {
            // Fundamentally we have a Razor half of the world as as soon as the document is open - and then later 
            // the C# half of the world will be initialized. This code is in general pretty tolerant of 
            // unexpected /impossible states.
            //
            // We also want to successfully shut down if the buffer is something other than .cshtml.
            IVsHierarchy hierarchy = null;
            string projectPath = null;
            var isSupportedProject = false;

            if (_textBuffer.ContentType.IsOfType(RazorLanguage.ContentType) &&

                // We expect the document to have a hierarchy even if it's not a real 'project'.
                // However the hierarchy can be null when the document is in the process of closing.
                (hierarchy = _projectService.GetHierarchy(_textBuffer)) != null)
            {
                projectPath = _projectService.GetProjectPath(hierarchy);
                isSupportedProject = _projectService.IsSupportedProject(hierarchy);
            }

            if (!isSupportedProject || projectPath == null)
            {
                return;
            }

            var project = _projectManager.GetProjectWithFilePath(projectPath);

            var subscription = _projectManager.Subscribe();
            subscription.ProjectChanged += Subscription_ProjectStateChanged;

            _isSupportedProject = isSupportedProject;
            _projectPath = projectPath;
            _project = project;
            _subscription = subscription;
        }
        
        private void OnContextChanged(ProjectSnapshot project)
        {
            _project = project;

            var handler = ContextChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void Subscription_ProjectStateChanged(object sender, ProjectChangeEventArgs e)
        {
            if (_projectPath != null &&
                string.Equals(_projectPath, e.Project.UnderlyingProject.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                OnContextChanged(e.Project);
            }
        }
    }
}
