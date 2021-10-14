// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperSpanSerializer
    {
        internal static string Serialize(RazorSyntaxTree syntaxTree)
        {
            using (var writer = new StringWriter())
            {
                var visitor = new TagHelperSpanWriter(writer, syntaxTree);
                visitor.Visit();

                return writer.ToString();
            }
        }
    }
}
