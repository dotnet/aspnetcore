// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(RazorTextBufferProvider), RazorLanguage.Name)]
    internal class DefaultTextBufferProviderFactory : ILanguageServiceFactory
    {
        private readonly IBufferGraphFactoryService _bufferGraphService;

        [ImportingConstructor]
        public DefaultTextBufferProviderFactory(IBufferGraphFactoryService bufferGraphService)
        {
            if (bufferGraphService == null)
            {
                throw new ArgumentNullException(nameof(bufferGraphService));
            }

            _bufferGraphService = bufferGraphService;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            return new DefaultTextBufferProvider(_bufferGraphService);
        }
    }
}
