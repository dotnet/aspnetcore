// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Razor directive completion provider.")]
    [ContentType(RazorLanguage.CoreContentType)]
    internal class RazorDirectiveCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public RazorDirectiveCompletionSourceProvider(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            var razorBuffer = textView.BufferGraph.GetRazorBuffers().FirstOrDefault();
            if (!razorBuffer.Properties.TryGetProperty(typeof(RazorDirectiveCompletionSource), out IAsyncCompletionSource completionSource))
            {
                completionSource = CreateCompletionSource(razorBuffer);
                razorBuffer.Properties.AddProperty(typeof(RazorDirectiveCompletionSource), completionSource);
            }

            return completionSource;
        }

        // Internal for testing
        internal IAsyncCompletionSource CreateCompletionSource(ITextBuffer razorBuffer)
        {
            if (!razorBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser))
            {
                // Parser hasn't been associated with the text buffer yet.
                return null;
            }

            var completionSource = new RazorDirectiveCompletionSource(parser, _foregroundDispatcher);
            return completionSource;
        }
    }
}
