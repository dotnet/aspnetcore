// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorCodeDocumentProvider))]
    internal class DefaultCodeDocumentProvider : RazorCodeDocumentProvider
    {
        private readonly RazorTextBufferProvider _bufferProvider;
        private readonly IEnumerable<TextBufferCodeDocumentProvider> _codeDocumentProviders;

        [ImportingConstructor]
        public DefaultCodeDocumentProvider(
            RazorTextBufferProvider bufferProvider, 
            [ImportMany] IEnumerable<TextBufferCodeDocumentProvider> codeDocumentProviders)
        {
            if (bufferProvider == null)
            {
                throw new ArgumentNullException(nameof(bufferProvider));
            }

            if (codeDocumentProviders == null)
            {
                throw new ArgumentNullException(nameof(codeDocumentProviders));
            }

            _bufferProvider = bufferProvider;
            _codeDocumentProviders = codeDocumentProviders;
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

            foreach (var codeDocumentProvider in _codeDocumentProviders)
            {
                if (codeDocumentProvider.TryGetFromBuffer(textBuffer, out codeDocument))
                {
                    return true;
                }
            }

            // A Razor code document has not yet been associated with the buffer yet.
            codeDocument = null;
            return false;
        }
    }
}
