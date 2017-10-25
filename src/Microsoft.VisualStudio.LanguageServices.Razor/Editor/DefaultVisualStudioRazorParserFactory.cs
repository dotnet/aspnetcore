// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Language.Intellisense;
using TemplateEngineFactoryService = Microsoft.CodeAnalysis.Razor.RazorTemplateEngineFactoryService;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [System.Composition.Shared]
    [Export(typeof(VisualStudioRazorParserFactory))]
    internal class DefaultVisualStudioRazorParserFactory : VisualStudioRazorParserFactory
    {
        private readonly ForegroundDispatcher _dispatcher;
        private readonly TemplateEngineFactoryService _templateEngineFactoryService;
        private readonly ICompletionBroker _completionBroker;
        private readonly ErrorReporter _errorReporter;

        [ImportingConstructor]
        public DefaultVisualStudioRazorParserFactory(
            ICompletionBroker completionBroker,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _completionBroker = completionBroker;
            _dispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
            _errorReporter = workspace.Services.GetRequiredService<ErrorReporter>();
            var razorLanguageServices = workspace.Services.GetLanguageServices(RazorLanguage.Name);
            _templateEngineFactoryService = razorLanguageServices.GetRequiredService<TemplateEngineFactoryService>();
        }

        public override VisualStudioRazorParser Create(VisualStudioDocumentTracker documentTracker)
        {
            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            _dispatcher.AssertForegroundThread();

            var parser = new DefaultVisualStudioRazorParser(
                _dispatcher,
                documentTracker,
                _templateEngineFactoryService,
                _errorReporter,
                _completionBroker);
            return parser;
        }
    }
}