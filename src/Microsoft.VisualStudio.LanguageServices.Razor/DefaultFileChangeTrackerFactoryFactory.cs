// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(FileChangeTrackerFactory), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultFileChangeTrackerFactoryFactory : ILanguageServiceFactory
    {
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public DefaultFileChangeTrackerFactoryFactory(ForegroundDispatcher foregroundDispatcher, SVsServiceProvider serviceProvider)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _fileChangeService = serviceProvider.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var errorReporter = languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>();
            return new DefaultFileChangeTrackerFactory(_foregroundDispatcher, errorReporter, _fileChangeService);
        }
    }
}
