// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [ExportLanguageServiceFactory(typeof(TextBufferCodeDocumentProvider), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultTextBufferCodeDocumentProviderFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new DefaultTextBufferCodeDocumentProvider();
        }
    }
}