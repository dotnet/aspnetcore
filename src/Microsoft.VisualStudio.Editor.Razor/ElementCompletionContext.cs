// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public sealed class ElementCompletionContext
    {
        public ElementCompletionContext(
            TagHelperDocumentContext documentContext,
            IEnumerable<string> existingCompletions,
            string containingTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string containingParentTagName,
            bool containingParentIsTagHelper,
            Func<string, bool> inHTMLSchema)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (existingCompletions == null)
            {
                throw new ArgumentNullException(nameof(existingCompletions));
            }

            if (inHTMLSchema == null)
            {
                throw new ArgumentNullException(nameof(inHTMLSchema));
            }

            DocumentContext = documentContext;
            ExistingCompletions = existingCompletions;
            ContainingTagName = containingTagName;
            Attributes = attributes;
            ContainingParentTagName = containingParentTagName;
            ContainingParentIsTagHelper = containingParentIsTagHelper;
            InHTMLSchema = inHTMLSchema;
        }

        public TagHelperDocumentContext DocumentContext { get; }

        public IEnumerable<string> ExistingCompletions { get; }

        public string ContainingTagName { get; }

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; }

        public string ContainingParentTagName { get; }

        public bool ContainingParentIsTagHelper { get; }

        public Func<string, bool> InHTMLSchema { get; }
    }
}
