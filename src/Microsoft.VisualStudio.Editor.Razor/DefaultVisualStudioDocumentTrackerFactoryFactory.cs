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
        private readonly ITextDocumentFactoryService _textDocumentFactory;

        [ImportingConstructor]
        public DefaultVisualStudioDocumentTrackerFactoryFactory(ForegroundDispatcher foregroundDispatcher, ITextDocumentFactoryService textDocumentFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
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
            var projectService = languageServices.GetRequiredService<TextBufferProjectService>();
            var importDocumentManager = languageServices.GetRequiredService<ImportDocumentManager>();

            return new DefaultVisualStudioDocumentTrackerFactory(
                _foregroundDispatcher,
                projectManager,
                workspaceEditorSettings,
                projectService,
                _textDocumentFactory,
                importDocumentManager,
                languageServices.WorkspaceServices.Workspace);
        }
    }
}
