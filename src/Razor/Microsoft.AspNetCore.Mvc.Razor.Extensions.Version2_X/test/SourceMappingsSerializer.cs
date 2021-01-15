// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public static class SourceMappingsSerializer
    {
        public static string Serialize(RazorCSharpDocument csharpDocument, RazorSourceDocument sourceDocument)
        {
            var builder = new StringBuilder();
            var sourceFilePath = sourceDocument.FilePath;
            var charBuffer = new char[sourceDocument.Length];
            sourceDocument.CopyTo(0, charBuffer, 0, sourceDocument.Length);
            var sourceContent = new string(charBuffer);

            for (var i = 0; i < csharpDocument.SourceMappings.Count; i++)
            {
                var sourceMapping = csharpDocument.SourceMappings[i];
                if (!string.Equals(sourceMapping.OriginalSpan.FilePath, sourceFilePath, StringComparison.Ordinal))
                {
                    continue;
                }

                builder.Append("Source Location: ");
                AppendMappingLocation(builder, sourceMapping.OriginalSpan, sourceContent);

                builder.Append("Generated Location: ");
                AppendMappingLocation(builder, sourceMapping.GeneratedSpan, csharpDocument.GeneratedCode);

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void AppendMappingLocation(StringBuilder builder, SourceSpan location, string content)
        {
            builder
                .AppendLine(location.ToString())
                .Append("|");

            for (var i = 0; i < location.Length; i++)
            {
                builder.Append(content[location.AbsoluteIndex + i]);
            }

            builder.AppendLine("|");
        }
    }
}
