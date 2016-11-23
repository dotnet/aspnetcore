// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Razor
{
    [ExportLanguageServiceFactory(typeof(TagHelperResolver), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultTagHelperResolverFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new DefaultTagHelperResolver();
        }
    }
}
