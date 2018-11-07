// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioDocumentTrackerFactory : VisualStudioDocumentTrackerFactory
    {
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly ProjectPathProvider _projectPathProvider;
        private readonly Workspace _workspace;
        private readonly ImportDocumentManager _importDocumentManager;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly WorkspaceEditorSettings _workspaceEditorSettings;

        public DefaultVisualStudioDocumentTrackerFactory(
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManager projectManager,
            WorkspaceEditorSettings workspaceEditorSettings,
            ProjectPathProvider projectPathProvider,
            ITextDocumentFactoryService textDocumentFactory,
            ImportDocumentManager importDocumentManager,
            Workspace workspace)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            if (workspaceEditorSettings == null)
            {
                throw new ArgumentNullException(nameof(workspaceEditorSettings));
            }

            if (projectPathProvider == null)
            {
                throw new ArgumentNullException(nameof(projectPathProvider));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (importDocumentManager == null)
            {
                throw new ArgumentNullException(nameof(importDocumentManager));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectManager = projectManager;
            _workspaceEditorSettings = workspaceEditorSettings;
            _projectPathProvider = projectPathProvider;
            _textDocumentFactory = textDocumentFactory;
            _importDocumentManager = importDocumentManager;
            _workspace = workspace;
        }

        public override VisualStudioDocumentTracker Create(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                Debug.Fail("Text document should be available from the text buffer.");
                return null;
            }

            if (!_projectPathProvider.TryGetProjectPath(textBuffer, out var projectPath))
            {
                return null;
            }

            var filePath = textDocument.FilePath;
            var tracker = new DefaultVisualStudioDocumentTracker(_foregroundDispatcher, filePath, projectPath, _projectManager, _workspaceEditorSettings, _workspace, textBuffer, _importDocumentManager);

            return tracker;
        }
    }
}
