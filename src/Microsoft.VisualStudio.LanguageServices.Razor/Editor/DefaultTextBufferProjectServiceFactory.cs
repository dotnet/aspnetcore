// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(TextBufferProjectService), RazorLanguage.Name)]
    internal class DefaultTextBufferProjectServiceFactory : ILanguageServiceFactory
    {
        private readonly RunningDocumentTable _documentTable;
        private readonly ITextDocumentFactoryService _documentFactory;

        [ImportingConstructor]
        public DefaultTextBufferProjectServiceFactory(
            SVsServiceProvider services,
            ITextDocumentFactoryService documentFactory)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _documentTable = new RunningDocumentTable(services);
            _documentFactory = documentFactory;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            return new DefaultTextBufferProjectService(_documentTable, _documentFactory);
        }
    }
}
