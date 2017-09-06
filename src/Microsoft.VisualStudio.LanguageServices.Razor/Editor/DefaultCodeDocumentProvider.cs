// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [System.Composition.Shared]
    [Export(typeof(RazorCodeDocumentProvider))]
    internal class DefaultCodeDocumentProvider : RazorCodeDocumentProvider
    {
        private readonly RazorTextBufferProvider _bufferProvider;
        private readonly VisualStudioCodeDocumentProvider _codeDocumentProvider;

        [ImportingConstructor]
        public DefaultCodeDocumentProvider(RazorTextBufferProvider bufferProvider, VisualStudioCodeDocumentProvider codeDocumentProvider)
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

            if (!_codeDocumentProvider.TryGetFromBuffer(textBuffer, out codeDocument))
            {
                // A Razor code document has not yet been associated with the buffer.
                return false;
            }

            return true;
        }
    }
}
