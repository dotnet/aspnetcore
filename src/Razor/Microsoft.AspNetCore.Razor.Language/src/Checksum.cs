// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class Checksum
{
    public static string BytesToString(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var result = new StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            // The x2 format means lowercase hex, where each byte is a 2-character string.
            result.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }
}
