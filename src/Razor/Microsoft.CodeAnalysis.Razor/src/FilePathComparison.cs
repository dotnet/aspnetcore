// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Razor;

internal static class FilePathComparison
{
    private static StringComparison? _instance;

    public static StringComparison Instance
    {
        get
        {
            if (_instance == null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _instance = StringComparison.Ordinal;
            }
            else if (_instance == null)
            {
                _instance = StringComparison.OrdinalIgnoreCase;
            }

            return _instance.Value;
        }
    }
}
