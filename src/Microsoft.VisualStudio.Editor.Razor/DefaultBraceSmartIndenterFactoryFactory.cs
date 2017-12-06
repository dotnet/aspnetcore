// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(BraceSmartIndenterFactory), RazorLanguage.Name, ServiceLayer.Default)]
    internal class DefaultBraceSmartIndenterFactoryFactory : ILanguageServiceFactory
    {
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;

        [ImportingConstructor]
        public DefaultBraceSmartIndenterFactoryFactory(IEditorOperationsFactoryService editorOperationsFactory)
        {
            if (editorOperationsFactory == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactory));
            }

            _editorOperationsFactory = editorOperationsFactory;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var dispatcher = languageServices.WorkspaceServices.GetRequiredService<ForegroundDispatcher>();
            return new DefaultBraceSmartIndenterFactory(dispatcher, _editorOperationsFactory);
        }
    }
}
