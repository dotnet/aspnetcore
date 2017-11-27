// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewConnectionListener))]
    internal class RazorTextViewConnectionListener : ITextViewConnectionListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorDocumentManager _documentManager;

        [ImportingConstructor]
        public RazorTextViewConnectionListener(
            VisualStudioWorkspaceAccessor workspaceAccessor,
            RazorDocumentManager documentManager)
        {
            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            _documentManager = documentManager;
            _foregroundDispatcher = workspaceAccessor.Workspace.Services.GetRequiredService<ForegroundDispatcher>();
        }

        // This is only for testing. We want to avoid using the actual Roslyn GetService methods in unit tests.
        internal RazorTextViewConnectionListener(
            ForegroundDispatcher foregroundDispatcher,
            RazorDocumentManager documentManager)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentManager = documentManager;
        }

        public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
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

        public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
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
