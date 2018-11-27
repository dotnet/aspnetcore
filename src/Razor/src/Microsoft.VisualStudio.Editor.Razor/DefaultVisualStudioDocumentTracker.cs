// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioDocumentTracker : VisualStudioDocumentTracker
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly string _filePath;
        private readonly string _projectPath;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly WorkspaceEditorSettings _workspaceEditorSettings;
        private readonly ITextBuffer _textBuffer;
        private readonly ImportDocumentManager _importDocumentManager;
        private readonly List<ITextView> _textViews;
        private readonly Workspace _workspace;
        private bool _isSupportedProject;
        private ProjectSnapshot _project;

        public override event EventHandler<ContextChangeEventArgs> ContextChanged;

        public DefaultVisualStudioDocumentTracker(
            ForegroundDispatcher dispatcher,
            string filePath,
            string projectPath,
            ProjectSnapshotManager projectManager,
            WorkspaceEditorSettings workspaceEditorSettings,
            Workspace workspace,
            ITextBuffer textBuffer,
            ImportDocumentManager importDocumentManager)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

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

            if (workspaceEditorSettings == null)
            {
                throw new ArgumentNullException(nameof(workspaceEditorSettings));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (importDocumentManager == null)
            {
                throw new ArgumentNullException(nameof(importDocumentManager));
            }

            _foregroundDispatcher = dispatcher;
            _filePath = filePath;
            _projectPath = projectPath;
            _projectManager = projectManager;
            _workspaceEditorSettings = workspaceEditorSettings;
            _textBuffer = textBuffer;
            _importDocumentManager = importDocumentManager;
            _workspace = workspace; // For now we assume that the workspace is the always default VS workspace.

            _textViews = new List<ITextView>();
        }

        public override RazorConfiguration Configuration => _project?.Configuration;

        public override EditorSettings EditorSettings => _workspaceEditorSettings.Current;

        public override IReadOnlyList<TagHelperDescriptor> TagHelpers => _project?.TagHelpers ?? Array.Empty<TagHelperDescriptor>();

        public override bool IsSupportedProject => _isSupportedProject;

        public override Project Project => _workspace.CurrentSolution.GetProject(_project.WorkspaceProject.Id);

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
            _importDocumentManager.OnSubscribed(this);

            _workspaceEditorSettings.Changed += EditorSettingsManager_Changed;
            _projectManager.Changed += ProjectManager_Changed;
            _importDocumentManager.Changed += Import_Changed;

            _isSupportedProject = true;
            _project = _projectManager.GetProjectWithFilePath(_projectPath);

            OnContextChanged(_project, ContextChangeKind.ProjectChanged);
        }

        public void Unsubscribe()
        {
            _importDocumentManager.OnUnsubscribed(this);

            _projectManager.Changed -= ProjectManager_Changed;
            _workspaceEditorSettings.Changed -= EditorSettingsManager_Changed;
            _importDocumentManager.Changed -= Import_Changed;

            // Detached from project.
            _isSupportedProject = false;
            _project = null;

            OnContextChanged(project: null, kind: ContextChangeKind.ProjectChanged);
        }

        private void OnContextChanged(ProjectSnapshot project, ContextChangeKind kind)
        {
            _foregroundDispatcher.AssertForegroundThread();

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
                string.Equals(_projectPath, e.Project.FilePath, StringComparison.OrdinalIgnoreCase))
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

        // Internal for testing
        internal void Import_Changed(object sender, ImportChangedEventArgs args)
        {
            foreach (var path in args.AssociatedDocuments)
            {
                if (string.Equals(_filePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    OnContextChanged(_project, ContextChangeKind.ImportsChanged);
                    break;
                }
            }
        }
    }
}
