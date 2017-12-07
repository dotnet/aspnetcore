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
        private readonly TextBufferProjectService _projectService;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly Workspace _workspace;
        private readonly ImportDocumentManager _importDocumentManager;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly EditorSettingsManagerInternal _editorSettingsManager;

        public DefaultVisualStudioDocumentTrackerFactory(
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManager projectManager,
            EditorSettingsManagerInternal editorSettingsManager,
            TextBufferProjectService projectService,
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

            if (editorSettingsManager == null)
            {
                throw new ArgumentNullException(nameof(editorSettingsManager));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
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
            _editorSettingsManager = editorSettingsManager;
            _projectService = projectService;
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

            var filePath = textDocument.FilePath;
            var project = _projectService.GetHostProject(textBuffer);
            if (project == null)
            {
                Debug.Fail("Text buffer should belong to a project.");
                return null;
            }

            var projectPath = _projectService.GetProjectPath(project);

            var tracker = new DefaultVisualStudioDocumentTracker(filePath, projectPath, _projectManager, _editorSettingsManager, _workspace, textBuffer, _importDocumentManager);

            return tracker;
        }
    }
}
