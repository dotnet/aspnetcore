// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(TextBufferCodeDocumentProvider))]
    internal class DefaultTextBufferCodeDocumentProvider : TextBufferCodeDocumentProvider
    {
        public override bool TryGetFromBuffer(ITextBuffer textBuffer, out RazorCodeDocument codeDocument)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // Hack until we own the lifetime of the parser.
            if (textBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser) && parser.CodeDocument != null)
            {
                codeDocument = parser.CodeDocument;
                return true;
            }

            codeDocument = null;
            return false;
        }
    }
}
