// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide.CodeCompletion;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.Editor
{
    internal class DefaultVisualStudioCompletionBroker : VisualStudioCompletionBroker
    {
        public override bool IsCompletionActive(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView.HasAggregateFocus)
            {
                return CompletionWindowManager.IsVisible ||
                                              (textView.Properties.TryGetProperty<bool>("RoslynCompletionPresenterSession.IsCompletionActive", out var visible)
                                               && visible);
            }

            // Text view does not have focus, if the completion window is visible it's for a different text view.
            return false;
        }
    }
}
