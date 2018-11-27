// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(VisualStudioDocumentTrackerFactory), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultVisualStudioDocumentTrackerFactoryFactory : ILanguageServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly TextBufferProjectService _projectService;
        private readonly ITextDocumentFactoryService _textDocumentFactory;

        [ImportingConstructor]
        public DefaultVisualStudioDocumentTrackerFactoryFactory(
            ForegroundDispatcher foregroundDispatcher,
            TextBufferProjectService projectService,
            ITextDocumentFactoryService textDocumentFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
            _textDocumentFactory = textDocumentFactory;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var projectManager = languageServices.GetRequiredService<ProjectSnapshotManager>();
            var workspaceEditorSettings = languageServices.GetRequiredService<WorkspaceEditorSettings>();
            var importDocumentManager = languageServices.GetRequiredService<ImportDocumentManager>();

            return new DefaultVisualStudioDocumentTrackerFactory(
                _foregroundDispatcher,
                projectManager,
                workspaceEditorSettings,
                _projectService,
                _textDocumentFactory,
                importDocumentManager,
                languageServices.WorkspaceServices.Workspace);
        }
    }
}
