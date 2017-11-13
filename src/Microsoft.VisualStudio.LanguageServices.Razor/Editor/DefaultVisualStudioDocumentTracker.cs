// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    internal class DefaultVisualStudioDocumentTracker : VisualStudioDocumentTracker
    {
        private readonly string _filePath;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly EditorSettingsManagerInternal _editorSettingsManager;
        private readonly TextBufferProjectService _projectService;
        private readonly ITextBuffer _textBuffer;
        private readonly List<ITextView> _textViews;
        private readonly Workspace _workspace;
        private bool _isSupportedProject;
        private ProjectSnapshot _project;
        private string _projectPath;

        public override event EventHandler ContextChanged;

        public DefaultVisualStudioDocumentTracker(
            string filePath,
            ProjectSnapshotManager projectManager,
            TextBufferProjectService projectService,
            EditorSettingsManagerInternal editorSettingsManager,
            Workspace workspace,
            ITextBuffer textBuffer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (editorSettingsManager == null)
            {
                throw new ArgumentNullException(nameof(editorSettingsManager));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _filePath = filePath;
            _projectManager = projectManager;
            _projectService = projectService;
            _editorSettingsManager = editorSettingsManager;
            _textBuffer = textBuffer;
            _workspace = workspace; // For now we assume that the workspace is the always default VS workspace.

            _textViews = new List<ITextView>();
        }

        internal override ProjectExtensibilityConfiguration Configuration => _project.Configuration;

        public override EditorSettings EditorSettings => _editorSettingsManager.Current;

        public override bool IsSupportedProject => _isSupportedProject;

        public override Project Project => _workspace.CurrentSolution.GetProject(_project.UnderlyingProject.Id);

        public override ITextBuffer TextBuffer => _textBuffer;

        public override IReadOnlyList<ITextView> TextViews => _textViews;

        public override Workspace Workspace => _workspace;

        public override string FilePath => _filePath;

        public override string ProjectPath => _projectPath;

        internal void AddTextView(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!_textViews.Contains(textView))
            {
                _textViews.Add(textView);

                if (_textViews.Count == 1)
                {
                    Subscribe();
                }
            }
        }

        internal void RemoveTextView(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (_textViews.Contains(textView))
            {
                _textViews.Remove(textView);

                if (_textViews.Count == 0)
                {
                    Unsubscribe();
                }
            }
        }

        public override ITextView GetFocusedTextView()
        {
            for (var i = 0; i < TextViews.Count; i++)
            {
                if (TextViews[i].HasAggregateFocus)
                {
                    return TextViews[i];
                }
            }

            return null;
        }

        private void Subscribe()
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

            _isSupportedProject = isSupportedProject;
            _projectPath = projectPath;
            _project = _projectManager.GetProjectWithFilePath(projectPath);
            _projectManager.Changed += ProjectManager_Changed;
            _editorSettingsManager.Changed += EditorSettingsManager_Changed;

            OnContextChanged(_project);
        }

        private void Unsubscribe()
        {
            _projectManager.Changed -= ProjectManager_Changed;
            _editorSettingsManager.Changed -= EditorSettingsManager_Changed;

            // Detached from project.
            _isSupportedProject = false;
            _project = null;
            OnContextChanged(project: null);
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

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            if (_projectPath != null &&
                string.Equals(_projectPath, e.Project.UnderlyingProject.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                OnContextChanged(e.Project);
            }
        }

        // Internal for testing
        internal void EditorSettingsManager_Changed(object sender, EditorSettingsChangedEventArgs args)
        {
            OnContextChanged(_project);
        }
    }
}
