// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

[assembly: AssemblyMetadata("ReceiveHotReloadDeltaNotification", "Microsoft.AspNetCore.Components.HotReload.HotReloadManager")]

namespace Microsoft.AspNetCore.Components.HotReload
{
    // Not to be confused with the HR Manager.
    internal static class HotReloadManager
    {
        // Hot reload stuff
        internal static bool IsHotReloadEnabled = Environment.GetEnvironmentVariable("COMPLUS_ForceEnc") == "1";

        // For hotreload
        internal static event Action? OnDeltaApplied;

        public static void DeltaApplied()
        {
            OnDeltaApplied?.Invoke();
        }
    }
}
