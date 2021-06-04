// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.HotReload;

[assembly: MetadataUpdateHandler(typeof(HotReloadManager))]

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal static class HotReloadManager
    {
       internal static event Action? OnDeltaApplied;

        public static void DeltaApplied()
        {
            OnDeltaApplied?.Invoke();
        }

        public static void UpdateApplication(Type[]? _) => OnDeltaApplied?.Invoke();
    }
}
