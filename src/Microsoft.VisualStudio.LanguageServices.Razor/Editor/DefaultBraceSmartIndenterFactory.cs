// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [System.Composition.Shared]
    [Export(typeof(BraceSmartIndenterFactory))]
    internal class DefaultBraceSmartIndenterFactory : BraceSmartIndenterFactory
    {
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly ForegroundDispatcher _dispatcher;

        [ImportingConstructor]
        public DefaultBraceSmartIndenterFactory(
            IEditorOperationsFactoryService editorOperationsFactory,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (editorOperationsFactory == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactory));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _editorOperationsFactory = editorOperationsFactory;
            _dispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
        }

        public override BraceSmartIndenter Create(VisualStudioDocumentTracker documentTracker)
        {
            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            _dispatcher.AssertForegroundThread();

            var braceSmartIndenter = new BraceSmartIndenter(_dispatcher, documentTracker, _editorOperationsFactory);

            return braceSmartIndenter;
        }
    }
}
