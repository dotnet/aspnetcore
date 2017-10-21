// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [System.Composition.Shared]
    [Export(typeof(VisualStudioDocumentTrackerFactory))]
    internal class DefaultVisualStudioDocumentTrackerFactory : VisualStudioDocumentTrackerFactory
    {
        private readonly TextBufferProjectService _projectService;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly Workspace _workspace;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly EditorSettingsManager _editorSettingsManager;

        [ImportingConstructor]
        public DefaultVisualStudioDocumentTrackerFactory(
            TextBufferProjectService projectService,
            ITextDocumentFactoryService textDocumentFactory,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _projectService = projectService;
            _textDocumentFactory = textDocumentFactory;
            _workspace = workspace;

            _foregroundDispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
            var razorLanguageServices = workspace.Services.GetLanguageServices(RazorLanguage.Name);
            _projectManager = razorLanguageServices.GetRequiredService<ProjectSnapshotManager>();
            _editorSettingsManager = razorLanguageServices.GetRequiredService<EditorSettingsManager>();
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
            var tracker = new DefaultVisualStudioDocumentTracker(filePath, _projectManager, _projectService, _editorSettingsManager, _workspace, textBuffer);

            return tracker;
        }
    }
}
