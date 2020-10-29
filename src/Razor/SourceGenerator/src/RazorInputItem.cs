// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis.Razor
{
    internal readonly struct RazorInputItem
    {
        public RazorInputItem(AdditionalText additionalText, string relativePath, string fileKind, string generatedOutputPath, string generatedDeclarationPath, string cssScope)
        {
            AdditionalText = additionalText;
            RelativePath = relativePath;
            CssScope = cssScope;
            NormalizedPath = '/' + relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace("//", "/");
            FileKind = fileKind;
            GeneratedOutputPath = generatedOutputPath;
            GeneratedDeclarationPath = generatedDeclarationPath;
        }

        public AdditionalText AdditionalText { get; }

        public string RelativePath { get; }

        public string NormalizedPath { get; }

        public string FileKind { get; }

        public string CssScope { get; }

        public string GeneratedOutputPath { get; }

        public string GeneratedDeclarationPath { get; }
    }
}
