// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    [System.Composition.Shared]
    [Export(typeof(TextBufferCodeDocumentProvider))]
    internal class LegacyTextBufferCodeDocumentProvider : TextBufferCodeDocumentProvider
    {
        public override bool TryGetFromBuffer(ITextBuffer textBuffer, out RazorCodeDocument codeDocument)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (textBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser) && parser.CodeDocument != null)
            {
                codeDocument = parser.CodeDocument;
                return true;
            }

            // Support the legacy parser for code document extraction.
            if (textBuffer.Properties.TryGetProperty(typeof(RazorEditorParser), out RazorEditorParser legacyParser) && legacyParser.CodeDocument != null)
            {
                codeDocument = legacyParser.CodeDocument;
                return true;
            }

            codeDocument = null;
            return false;
        }
    }
}
