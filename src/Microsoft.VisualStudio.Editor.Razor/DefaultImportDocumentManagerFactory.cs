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
    [ExportLanguageServiceFactory(typeof(ImportDocumentManager), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultImportDocumentManagerFactory : ILanguageServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public DefaultImportDocumentManagerFactory(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var errorReporter = languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>();
            var fileChangeTrackerFactory = languageServices.GetRequiredService<FileChangeTrackerFactory>();
            var templateEngineFactoryService = languageServices.GetRequiredService<RazorTemplateEngineFactoryService>();

            return new DefaultImportDocumentManager(
                _foregroundDispatcher,
                errorReporter,
                fileChangeTrackerFactory,
                templateEngineFactoryService);
        }
    }
}
