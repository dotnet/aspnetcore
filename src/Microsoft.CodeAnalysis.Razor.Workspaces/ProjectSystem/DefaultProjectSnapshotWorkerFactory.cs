// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(ProjectSnapshotWorker), RazorLanguage.Name)]
    internal class DefaultProjectSnapshotWorkerFactory : ILanguageServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public DefaultProjectSnapshotWorkerFactory(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new System.ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new DefaultProjectSnapshotWorker(
                _foregroundDispatcher,
                languageServices.GetRequiredService<ProjectExtensibilityConfigurationFactory>(),
                languageServices.GetRequiredService<TagHelperResolver>());
        }
    }
}
