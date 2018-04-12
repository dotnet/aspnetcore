// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [ExportLanguageServiceFactory(typeof(TagHelperResolver), RazorLanguage.Name, ServiceLayer.Host)]
    internal class OOPTagHelperResolverFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new OOPTagHelperResolver(
                languageServices.WorkspaceServices.GetRequiredService<ProjectSnapshotProjectEngineFactory>(),
                languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>(),
                languageServices.WorkspaceServices.Workspace);
        }
    }
}