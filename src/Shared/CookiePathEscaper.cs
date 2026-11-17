// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Internal;

internal static class CookiePathEscaper
{
    public static string? Escape(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains(';'))
        {
            return value;
        }

        return value.Replace(";", "%3B", StringComparison.Ordinal);
    }
}
