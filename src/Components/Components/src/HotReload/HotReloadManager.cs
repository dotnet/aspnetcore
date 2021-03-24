// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

[assembly: AssemblyMetadata("ReceiveHotReloadDeltaNotification", "Microsoft.AspNetCore.Components.HotReload.HotReloadManager")]

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal static class HotReloadManager
    {
       internal static event Action? OnDeltaApplied;

        public static void DeltaApplied()
        {
            OnDeltaApplied?.Invoke();
        }
    }
}
