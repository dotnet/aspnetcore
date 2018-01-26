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
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorEditorFactoryService _editorFactoryService;

        [ImportingConstructor]
        public DefaultRazorDocumentManagerFactory(ForegroundDispatcher foregroundDispatcher, RazorEditorFactoryService editorFactoryService)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (editorFactoryService == null)
            {
                throw new ArgumentNullException(nameof(editorFactoryService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _editorFactoryService = editorFactoryService;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var projectService = languageServices.GetRequiredService<TextBufferProjectService>();

            return new DefaultRazorDocumentManager(_foregroundDispatcher, _editorFactoryService, projectService);
        }
    }
}
