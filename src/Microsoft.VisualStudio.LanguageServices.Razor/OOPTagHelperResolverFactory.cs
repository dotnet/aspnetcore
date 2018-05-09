// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [ExportLanguageServiceFactory(typeof(TagHelperResolver), RazorLanguage.Name, ServiceLayer.Host)]
    internal class OOPTagHelperResolverFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (!IsRemoteClientWorking())
            {
                return new DefaultTagHelperResolver();
            }

            return new OOPTagHelperResolver(
                languageServices.WorkspaceServices.GetRequiredService<ProjectSnapshotProjectEngineFactory>(),
                languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>(),
                languageServices.WorkspaceServices.Workspace);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool IsRemoteClientWorking()
        {
            try
            {
                LoadType();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LoadType()
        {
            // During 15.8 Roslyn renamed our OOP client from RazorLangaugeServiceClient to RazorLanguageServiceClient.
            GC.KeepAlive(typeof(RazorLangaugeServiceClient));
        }
    }
}