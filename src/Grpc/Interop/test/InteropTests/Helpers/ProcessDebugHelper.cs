// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace InteropTests.Helpers;

public static class ProcessDebugHelper
{
    public static string GetDebugCommand(ProcessStartInfo psi)
    {
        // Quote the file name if it contains spaces or special characters
        var fileName = QuoteIfNeeded(psi.FileName);

        // Arguments are typically already passed as a single string
        var arguments = psi.Arguments;

        return $"{fileName} {arguments}".Trim();
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        // Add quotes if value contains spaces or special characters
        if (value.Contains(' ') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        return value;
    }
}
