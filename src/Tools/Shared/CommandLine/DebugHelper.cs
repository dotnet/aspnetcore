// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public static class DebugHelper
{
    [Conditional("DEBUG")]
    public static void HandleDebugSwitch(ref string[] args)
    {
        if (args.Length > 0 && string.Equals("--debug", args[0], StringComparison.OrdinalIgnoreCase))
        {
            args = args.Skip(1).ToArray();
            Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
#if NET6_0_OR_GREATER
            Console.WriteLine($"Process ID: {Environment.ProcessId}");
#else
            Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
#endif
            Console.ReadLine();
        }
    }
}
