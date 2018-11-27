// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(ProjectSnapshotManager), RazorLanguage.Name)]
    internal class DefaultProjectSnapshotManagerFactory : ILanguageServiceFactory
    {
        private readonly IEnumerable<ProjectSnapshotChangeTrigger> _triggers;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public DefaultProjectSnapshotManagerFactory(
            ForegroundDispatcher foregroundDispatcher,
            [ImportMany] IEnumerable<ProjectSnapshotChangeTrigger> triggers)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (triggers == null)
            {
                throw new ArgumentNullException(nameof(triggers));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _triggers = triggers;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            return new DefaultProjectSnapshotManager(
                _foregroundDispatcher,
                languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>(),
                languageServices.GetRequiredService<ProjectSnapshotWorker>(),
                _triggers, 
                languageServices.WorkspaceServices.Workspace);
        }
    }
}
