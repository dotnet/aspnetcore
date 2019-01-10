// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DEBUG
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal static class DebugHelper
    {
        public static void HandleDebugSwitch(ref string[] args)
        {
            if (args.Length > 0 && string.Equals("--debug", args[0], StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
                Console.ReadLine();
            }
        }
    }
}
#endif