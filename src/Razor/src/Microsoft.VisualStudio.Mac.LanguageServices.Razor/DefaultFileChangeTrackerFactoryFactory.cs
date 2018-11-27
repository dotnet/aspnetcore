// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(FileChangeTrackerFactory), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultFileChangeTrackerFactoryFactory : ILanguageServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public DefaultFileChangeTrackerFactoryFactory(ForegroundDispatcher foregroundDispatcher)
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

            return new DefaultFileChangeTrackerFactory(_foregroundDispatcher);
        }
    }
}
