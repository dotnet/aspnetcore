// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Internal
{
    public static class RazorCodeDocumentExtensions
    {
        private const string RelativePathKey = "relative-path";

        public static string GetRelativePath(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.Items[RelativePathKey] as string;
        }


        public static void SetRelativePath(this RazorCodeDocument document, string relativePath)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[RelativePathKey] = relativePath;
        }
    }
}
