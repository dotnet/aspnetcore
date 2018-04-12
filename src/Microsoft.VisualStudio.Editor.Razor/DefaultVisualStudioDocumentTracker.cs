// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private ProjectSnapshot _projectSnapshot;

        // Only allow a single tag helper computation task at a time.
        private (ProjectSnapshot project, Task task) _computingTagHelpers;

        // Stores the result from the last time we computed tag helpers.
        private IReadOnlyList<TagHelperDescriptor> _tagHelpers;

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
            _tagHelpers = Array.Empty<TagHelperDescriptor>();
        }

        public override RazorConfiguration Configuration => _projectSnapshot?.Configuration;

        public override EditorSettings EditorSettings => _workspaceEditorSettings.Current;

        public override IReadOnlyList<TagHelperDescriptor> TagHelpers => _tagHelpers;

        public override bool IsSupportedProject => _isSupportedProject;

        public override Project Project =>
            _projectSnapshot.WorkspaceProject == null ?
            null :
            _workspace.CurrentSolution.GetProject(_projectSnapshot.WorkspaceProject.Id);

        internal override ProjectSnapshot ProjectSnapshot => _projectSnapshot;

        public override ITextBuffer TextBuffer => _textBuffer;

        public override IReadOnlyList<ITextView> TextViews => _textViews;

        public override Workspace Workspace => _workspace;

        public override string FilePath => _filePath;

        public override string ProjectPath => _projectPath;

        public Task PendingTagHelperTask => _computingTagHelpers.task ?? Task.CompletedTask;

        internal void AddTextView(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            _foregroundDispatcher.AssertForegroundThread();

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

            _foregroundDispatcher.AssertForegroundThread();

            if (_textViews.Contains(textView))
            {
                _textViews.Remove(textView);
            }
        }

        public override ITextView GetFocusedTextView()
        {
            _foregroundDispatcher.AssertForegroundThread();

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
            _foregroundDispatcher.AssertForegroundThread();

            _projectSnapshot = _projectManager.GetOrCreateProject(_projectPath);
            _isSupportedProject = true;

            _projectManager.Changed += ProjectManager_Changed;
            _workspaceEditorSettings.Changed += EditorSettingsManager_Changed;
            _importDocumentManager.Changed += Import_Changed;
            _importDocumentManager.OnSubscribed(this);

            OnContextChanged(ContextChangeKind.ProjectChanged);
        }

        public void Unsubscribe()
        {
            _foregroundDispatcher.AssertForegroundThread();

            _importDocumentManager.OnUnsubscribed(this);

            _projectManager.Changed -= ProjectManager_Changed;
            _workspaceEditorSettings.Changed -= EditorSettingsManager_Changed;
            _importDocumentManager.Changed -= Import_Changed;

            // Detached from project.
            _isSupportedProject = false;
            _projectSnapshot = null;
            OnContextChanged(kind: ContextChangeKind.ProjectChanged);
        }

        private void StartComputingTagHelpers()
        {
            _foregroundDispatcher.AssertForegroundThread();

            Debug.Assert(_projectSnapshot != null);
            Debug.Assert(_computingTagHelpers.project == null && _computingTagHelpers.task == null);

            if (_projectSnapshot.TryGetTagHelpers(out var results))
            {
                _tagHelpers = results;
                OnContextChanged(ContextChangeKind.TagHelpersChanged);
                return;
            }

            // if we get here then we know the tag helpers aren't available, so force async for ease of testing
            var task = _projectSnapshot
                .GetTagHelpersAsync()
                .ContinueWith(TagHelpersUpdated, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, _foregroundDispatcher.ForegroundScheduler);
            _computingTagHelpers = (_projectSnapshot, task);
        }

        private void TagHelpersUpdated(Task<IReadOnlyList<TagHelperDescriptor>> task)
        {
            _foregroundDispatcher.AssertForegroundThread();

            Debug.Assert(_computingTagHelpers.project != null && _computingTagHelpers.task != null);

            if (!_isSupportedProject)
            {
                return;
            }

            _tagHelpers = task.Exception == null ? task.Result : Array.Empty<TagHelperDescriptor>();
            OnContextChanged(ContextChangeKind.TagHelpersChanged);

            var projectHasChanges = _projectSnapshot != null && _projectSnapshot != _computingTagHelpers.project;
            _computingTagHelpers = (null, null);

            if (projectHasChanges)
            {
                // More changes, keep going.
                StartComputingTagHelpers();
            }
        }

        private void OnContextChanged(ContextChangeKind kind)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var handler = ContextChanged;
            if (handler != null)
            {
                handler(this, new ContextChangeEventArgs(kind));
            }

            if (kind == ContextChangeKind.ProjectChanged && 
                _projectSnapshot != null &&
                _computingTagHelpers.project == null)
            {
                StartComputingTagHelpers();
            }
        }

        // Internal for testing
        internal void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (_projectPath != null &&
                string.Equals(_projectPath, e.ProjectFilePath, StringComparison.OrdinalIgnoreCase))
            {
                // This will be the new snapshot unless the project was removed.
                _projectSnapshot = _projectManager.GetLoadedProject(e.ProjectFilePath);

                switch (e.Kind)
                {
                    case ProjectChangeKind.DocumentsChanged:

                        // Nothing to do.
                        break;

                    case ProjectChangeKind.ProjectAdded:
                    case ProjectChangeKind.ProjectChanged:

                        // Just an update
                        OnContextChanged(ContextChangeKind.ProjectChanged);
                        break;

                    case ProjectChangeKind.ProjectRemoved:

                        // Fall back to ephemeral project
                        _projectSnapshot = _projectManager.GetOrCreateProject(ProjectPath);
                        OnContextChanged(ContextChangeKind.ProjectChanged);
                        break;

                    case ProjectChangeKind.DocumentContentChanged:

                        // Do nothing
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown ProjectChangeKind {e.Kind}");
                }
            }
        }

        // Internal for testing
        internal void EditorSettingsManager_Changed(object sender, EditorSettingsChangedEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            OnContextChanged(ContextChangeKind.EditorSettingsChanged);
        }

        // Internal for testing
        internal void Import_Changed(object sender, ImportChangedEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            foreach (var path in args.AssociatedDocuments)
            {
                if (string.Equals(_filePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    OnContextChanged(ContextChangeKind.ImportsChanged);
                    break;
                }
            }
        }
    }
}
