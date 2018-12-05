// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultBraceSmartIndenterFactory : BraceSmartIndenterFactory
    {
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly ForegroundDispatcher _dispatcher;
        private readonly TextBufferCodeDocumentProvider _codeDocumentProvider;

        public DefaultBraceSmartIndenterFactory(
            ForegroundDispatcher dispatcher,
            TextBufferCodeDocumentProvider codeDocumentProvider,
            IEditorOperationsFactoryService editorOperationsFactory)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (codeDocumentProvider == null)
            {
                throw new ArgumentNullException(nameof(codeDocumentProvider));
            }

            if (editorOperationsFactory == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactory));
            }

            _dispatcher = dispatcher;
            _codeDocumentProvider = codeDocumentProvider;
            _editorOperationsFactory = editorOperationsFactory;
        }

        public override BraceSmartIndenter Create(VisualStudioDocumentTracker documentTracker)
        {
            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            _dispatcher.AssertForegroundThread();

            var braceSmartIndenter = new BraceSmartIndenter(_dispatcher, documentTracker, _codeDocumentProvider, _editorOperationsFactory);

            return braceSmartIndenter;
        }
    }
}
