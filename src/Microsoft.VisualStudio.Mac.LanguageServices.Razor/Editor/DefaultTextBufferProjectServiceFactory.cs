// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.Editor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(TextBufferProjectService), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultTextBufferProjectServiceFactory : ILanguageServiceFactory
    {
        private readonly ITextDocumentFactoryService _documentFactory;

        [ImportingConstructor]
        public DefaultTextBufferProjectServiceFactory(ITextDocumentFactoryService documentFactory)
        {
            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _documentFactory = documentFactory;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var errorReporter = languageServices.WorkspaceServices.GetRequiredService<ErrorReporter>();

            return new DefaultTextBufferProjectService(_documentFactory, errorReporter);
        }
    }
}
