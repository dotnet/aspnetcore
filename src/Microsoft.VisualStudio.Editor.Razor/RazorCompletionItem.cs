// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal sealed class RazorCompletionItem
    {
        public RazorCompletionItem(
            string displayText, 
            string insertText, 
            string description, 
            RazorCompletionItemKind kind)
        {
            if (displayText == null)
            {
                throw new ArgumentNullException(nameof(displayText));
            }

            if (insertText == null)
            {
                throw new ArgumentNullException(nameof(insertText));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            DisplayText = displayText;
            InsertText = insertText;
            Description = description;
            Kind = kind;
        }

        public string DisplayText { get; }

        public string InsertText { get; }

        public string Description { get; }

        public RazorCompletionItemKind Kind { get; }
    }
}
