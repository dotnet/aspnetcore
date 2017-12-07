// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(RazorDocumentManager), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultRazorDocumentManagerFactory : ILanguageServiceFactory
    {
        private readonly RazorEditorFactoryService _editorFactoryService;
        private readonly TextBufferProjectService _projectService;

        [ImportingConstructor]
        public DefaultRazorDocumentManagerFactory(
            RazorEditorFactoryService editorFactoryService,
            TextBufferProjectService projectService)
        {
            if (editorFactoryService == null)
            {
                throw new ArgumentNullException(nameof(editorFactoryService));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _editorFactoryService = editorFactoryService;
            _projectService = projectService;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var dispatcher = languageServices.WorkspaceServices.GetRequiredService<ForegroundDispatcher>();
            return new DefaultRazorDocumentManager(dispatcher, _editorFactoryService, _projectService);
        }
    }
}
