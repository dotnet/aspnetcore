// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(RazorIndentationFactsService), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultRazorIndentationFactsServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new DefaultRazorIndentationFactsService();
        }
    }
}
