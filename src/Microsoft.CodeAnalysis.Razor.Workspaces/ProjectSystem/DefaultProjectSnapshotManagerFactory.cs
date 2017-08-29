// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(ProjectSnapshotManager), RazorLanguage.Name)]
    internal class DefaultProjectSnapshotManagerFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            return new DefaultProjectSnapshotManager(languageServices.WorkspaceServices.Workspace);
        }
    }
}
