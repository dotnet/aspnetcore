// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class RazorCodeDocumentProvider
    {
        public abstract bool TryGetFromDocument(TextDocument document, out RazorCodeDocument codeDocument);
    }
}
