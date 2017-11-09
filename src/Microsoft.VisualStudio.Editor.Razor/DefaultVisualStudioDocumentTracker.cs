// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioDocumentTracker : VisualStudioDocumentTracker
    {
        private readonly string _filePath;
        private readonly string _projectPath;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly EditorSettingsManagerInternal _editorSettingsManager;
        private readonly ITextBuffer _textBuffer;
        private readonly List<ITextView> _textViews;
        private readonly Workspace _workspace;
        private bool _isSupportedProject;
        private ProjectSnapshot _project;

        public override event EventHandler<ContextChangeEventArgs> ContextChanged;

        public DefaultVisualStudioDocumentTracker(
            string filePath,
            string projectPath,
            ProjectSnapshotManager projectManager,
            EditorSettingsManagerInternal editorSettingsManager,
            Workspace workspace,
            ITextBuffer textBuffer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
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
            _projectPath = projectPath;
            _projectManager = projectManager;
            _editorSettingsManager = editorSettingsManager;
            _textBuffer = textBuffer;
            _workspace = workspace; // For now we assume that the workspace is the always default VS workspace.

            _textViews = new List<ITextView>();
        }

        internal override ProjectExtensibilityConfiguration Configuration => _project?.Configuration;

        public override EditorSettings EditorSettings => _editorSettingsManager.Current;

        public override IReadOnlyList<TagHelperDescriptor> TagHelpers => _project?.TagHelpers ?? Array.Empty<TagHelperDescriptor>();

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

        public void Subscribe()
        {
            _editorSettingsManager.Changed += EditorSettingsManager_Changed;
            _projectManager.Changed += ProjectManager_Changed;

            _isSupportedProject = true;
            _project = _projectManager.GetProjectWithFilePath(_projectPath);

            OnContextChanged(_project, ContextChangeKind.ProjectChanged);
        }

        public void Unsubscribe()
        {
            _projectManager.Changed -= ProjectManager_Changed;
            _editorSettingsManager.Changed -= EditorSettingsManager_Changed;

            // Detached from project.
            _isSupportedProject = false;
            _project = null;

            OnContextChanged(project: null, kind: ContextChangeKind.ProjectChanged);
        }

        private void OnContextChanged(ProjectSnapshot project, ContextChangeKind kind)
        {
            _project = project;

            var handler = ContextChanged;
            if (handler != null)
            {
                handler(this, new ContextChangeEventArgs(kind));
            }
        }

        // Internal for testing
        internal void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            if (_projectPath != null &&
                string.Equals(_projectPath, e.Project.UnderlyingProject.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (e.Kind == ProjectChangeKind.TagHelpersChanged)
                {
                    OnContextChanged(e.Project, ContextChangeKind.TagHelpersChanged);
                }
                else
                {
                    OnContextChanged(e.Project, ContextChangeKind.ProjectChanged);
                }
            }
        }

        // Internal for testing
        internal void EditorSettingsManager_Changed(object sender, EditorSettingsChangedEventArgs args)
        {
            OnContextChanged(_project, ContextChangeKind.EditorSettingsChanged);
        }
    }
}
