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
        private readonly TextBufferProjectService _projectService;
        private readonly ITextDocumentFactoryService _textDocumentFactory;

        [ImportingConstructor]
        public DefaultVisualStudioDocumentTrackerFactoryFactory(
            TextBufferProjectService projectService,
            ITextDocumentFactoryService textDocumentFactory)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            _projectService = projectService;
            _textDocumentFactory = textDocumentFactory;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var dispatcher = languageServices.WorkspaceServices.GetRequiredService<ForegroundDispatcher>();
            var projectManager = languageServices.GetRequiredService<ProjectSnapshotManager>();
            var editorSettingsManager = languageServices.GetRequiredService<EditorSettingsManagerInternal>();
            var importDocumentManager = languageServices.GetRequiredService<ImportDocumentManager>();

            return new DefaultVisualStudioDocumentTrackerFactory(
                dispatcher,
                projectManager,
                editorSettingsManager,
                _projectService,
                _textDocumentFactory,
                importDocumentManager,
                languageServices.WorkspaceServices.Workspace);
        }
    }
}
