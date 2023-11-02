// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.InternalTesting;

public static partial class DumpCollector
{
    public static void Collect(Process process, string fileName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Windows.Collect(process, fileName);
        }
        // No implementations yet for macOS and Linux
    }
}
