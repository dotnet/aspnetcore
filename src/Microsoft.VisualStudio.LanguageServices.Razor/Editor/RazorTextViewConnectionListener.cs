// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(IWpfTextViewConnectionListener))]
    internal class RazorTextViewConnectionListener : IWpfTextViewConnectionListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly Workspace _workspace;
        private readonly RazorDocumentManager _documentManager;

        [ImportingConstructor]
        public RazorTextViewConnectionListener(
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            RazorDocumentManager documentManager)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            _workspace = workspace;
            _documentManager = documentManager;
            _foregroundDispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
        }

        // This is only for testing. We want to avoid using the actual Roslyn GetService methods in unit tests.
        internal RazorTextViewConnectionListener(
            ForegroundDispatcher foregroundDispatcher,
            Workspace workspace,
            RazorDocumentManager documentManager)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _workspace = workspace;
            _documentManager = documentManager;
        }

        public Workspace Workspace => _workspace;

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            _documentManager.OnTextViewOpened(textView, subjectBuffers);
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            _documentManager.OnTextViewClosed(textView, subjectBuffers);
        }
    }
}
