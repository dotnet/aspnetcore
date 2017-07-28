// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public class AttributeCompletionContext
    {
        public AttributeCompletionContext(
            TagHelperDocumentContext documentContext,
            IEnumerable<string> existingCompletions,
            string currentTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string currentParentTagName,
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

            if (currentTagName == null)
            {
                throw new ArgumentNullException(nameof(currentTagName));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            if (inHTMLSchema == null)
            {
                throw new ArgumentNullException(nameof(inHTMLSchema));
            }

            DocumentContext = documentContext;
            ExistingCompletions = existingCompletions;
            CurrentTagName = currentTagName;
            Attributes = attributes;
            CurrentParentTagName = currentParentTagName;
            InHTMLSchema = inHTMLSchema;
        }

        public TagHelperDocumentContext DocumentContext { get; }

        public IEnumerable<string> ExistingCompletions { get; }

        public string CurrentTagName { get; }

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; }

        public string CurrentParentTagName { get; }

        public Func<string, bool> InHTMLSchema { get; }
    }
}