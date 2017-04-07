// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public static class LineMappingsSerializer
    {
        public static string Serialize(RazorCSharpDocument csharpDocument, RazorSourceDocument sourceDocument)
        {
            var builder = new StringBuilder();
            var charBuffer = new char[sourceDocument.Length];
            sourceDocument.CopyTo(0, charBuffer, 0, sourceDocument.Length);
            var sourceContent = new string(charBuffer);

            for (var i = 0; i < csharpDocument.LineMappings.Count; i++)
            {
                var lineMapping = csharpDocument.LineMappings[i];

                builder.Append("Source Location: ");
                AppendMappingLocation(builder, lineMapping.OriginalSpan, sourceContent);

                builder.Append("Generated Location: ");
                AppendMappingLocation(builder, lineMapping.GeneratedSpan, csharpDocument.GeneratedCode);

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
