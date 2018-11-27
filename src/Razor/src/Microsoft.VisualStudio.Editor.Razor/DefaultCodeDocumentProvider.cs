// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorCodeDocumentProvider))]
    internal class DefaultCodeDocumentProvider : RazorCodeDocumentProvider
    {
        private readonly RazorTextBufferProvider _bufferProvider;
        private readonly TextBufferCodeDocumentProvider _codeDocumentProvider;

        [ImportingConstructor]
        public DefaultCodeDocumentProvider(
            RazorTextBufferProvider bufferProvider,
            TextBufferCodeDocumentProvider codeDocumentProvider)
        {
            if (bufferProvider == null)
            {
                throw new ArgumentNullException(nameof(bufferProvider));
            }

            if (codeDocumentProvider == null)
            {
                throw new ArgumentNullException(nameof(codeDocumentProvider));
            }

            _bufferProvider = bufferProvider;
            _codeDocumentProvider = codeDocumentProvider;
        }

        public override bool TryGetFromDocument(TextDocument document, out RazorCodeDocument codeDocument)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (!_bufferProvider.TryGetFromDocument(document, out var textBuffer))
            {
                // Could not find a Razor buffer associated with the document.
                codeDocument = null;
                return false;
            }

            if (_codeDocumentProvider.TryGetFromBuffer(textBuffer, out codeDocument))
            {
                return true;
            }

            // A Razor code document has not yet been associated with the buffer yet.
            codeDocument = null;
            return false;
        }
    }
}
