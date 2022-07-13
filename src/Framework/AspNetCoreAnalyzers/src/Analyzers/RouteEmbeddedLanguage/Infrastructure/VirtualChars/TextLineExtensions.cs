// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;

internal static class TextLineExtensions
{
    public static int? GetFirstNonWhitespaceOffset(this TextLine line)
    {
        var text = line.Text;
        if (text != null)
        {
            for (var i = line.Start; i < line.End; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i - line.Start;
                }
            }
        }

        return null;
    }
}
