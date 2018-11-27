// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorTextBufferProvider))]
    internal class DefaultTextBufferProvider : RazorTextBufferProvider
    {
        private readonly IBufferGraphFactoryService _bufferGraphService;

        [ImportingConstructor]
        public DefaultTextBufferProvider(IBufferGraphFactoryService bufferGraphService)
        {
            if (bufferGraphService == null)
            {
                throw new ArgumentNullException(nameof(bufferGraphService));
            }

            _bufferGraphService = bufferGraphService;
        }

        public override bool TryGetFromDocument(TextDocument document, out ITextBuffer textBuffer)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            textBuffer = null;

            if (!document.TryGetText(out var sourceText))
            {
                // Could not retrieve source text from the document. We have no way have locating an ITextBuffer.
                return false;
            }

            var container = sourceText.Container;
            ITextBuffer buffer;
            try
            {
                buffer = container.GetTextBuffer();
            }
            catch (ArgumentException)
            {
                // The source text container was not built from an ITextBuffer.
                return false;
            }

            var bufferGraph = _bufferGraphService.CreateBufferGraph(buffer);
            var razorBuffer = bufferGraph.GetRazorBuffers().FirstOrDefault();

            if (razorBuffer == null)
            {
                // Could not find a text buffer associated with the text document.
                return false;
            }

            textBuffer = razorBuffer;
            return true;
        }
    }
}
