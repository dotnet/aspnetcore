// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public abstract class TextViewRazorDocumentTrackerService
    {
        public abstract RazorDocumentTracker CreateTracker(ITextView textView);
    }
}
