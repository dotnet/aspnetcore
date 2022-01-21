// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

// This code is a stop-gap and exists to address the issues with extracting
// original method names from generated local functions. See https://github.com/dotnet/roslyn/issues/55651
// for more info.
namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal static class GeneratedNameParser
{
    /// <summary>
    /// Parses generated local function name out of a generated method name.
    /// </summary>
    internal static bool TryParseLocalFunctionName(string generatedName, [NotNullWhen(true)] out string? originalName)
    {
        originalName = null;

        var startIndex = generatedName.LastIndexOf(">g__", StringComparison.Ordinal);
        var endIndex = generatedName.LastIndexOf("|", StringComparison.Ordinal);
        if (startIndex >= 0 && endIndex >= 0 && endIndex - startIndex > 4)
        {
            originalName = generatedName.Substring(startIndex + 4, endIndex - startIndex - 4);
            return true;
        }

        return false;
    }
}
