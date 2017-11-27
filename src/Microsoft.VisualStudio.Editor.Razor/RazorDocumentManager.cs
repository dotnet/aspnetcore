// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class RazorDocumentManager
    {
        public abstract void OnTextViewOpened(ITextView textView, IEnumerable<ITextBuffer> subjectBuffers);

        public abstract void OnTextViewClosed(ITextView textView, IEnumerable<ITextBuffer> subjectBuffers);
    }
}
