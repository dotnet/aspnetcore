// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Text
{
    internal static class SourceTextExtensions
    {
        public static RazorSourceDocument GetRazorSourceDocument(this SourceText sourceText, string fileName)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var content = sourceText.ToString();

            return RazorSourceDocument.Create(content, fileName);
        }
    }
}
