// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioRazorParserFactory : VisualStudioRazorParserFactory
    {
        private readonly ForegroundDispatcher _dispatcher;
        private readonly RazorProjectEngineFactoryService _projectEngineFactoryService;
        private readonly VisualStudioCompletionBroker _completionBroker;
        private readonly ErrorReporter _errorReporter;

        public DefaultVisualStudioRazorParserFactory(
            ForegroundDispatcher dispatcher,
            ErrorReporter errorReporter,
            VisualStudioCompletionBroker completionBroker,
            RazorProjectEngineFactoryService projectEngineFactoryService)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            if (projectEngineFactoryService == null)
            {
                throw new ArgumentNullException(nameof(projectEngineFactoryService));
            }

            _dispatcher = dispatcher;
            _errorReporter = errorReporter;
            _completionBroker = completionBroker;
            _projectEngineFactoryService = projectEngineFactoryService;
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
                _projectEngineFactoryService,
                _errorReporter,
                _completionBroker);
            return parser;
        }
    }
}