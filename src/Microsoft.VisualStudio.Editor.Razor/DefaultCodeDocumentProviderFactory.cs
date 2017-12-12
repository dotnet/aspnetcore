// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(RazorCodeDocumentProvider), RazorLanguage.Name)]
    internal class DefaultCodeDocumentProviderFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var textBufferProvider = languageServices.GetRequiredService<RazorTextBufferProvider>();
            var textBufferCodeDocumentProvider = languageServices.GetRequiredService<TextBufferCodeDocumentProvider>();

            return new DefaultCodeDocumentProvider(textBufferProvider, textBufferCodeDocumentProvider);
        }
    }
}
