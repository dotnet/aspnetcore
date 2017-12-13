// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(TagHelperResolver), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultTagHelperResolverFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new DefaultTagHelperResolver();
        }
    }
}