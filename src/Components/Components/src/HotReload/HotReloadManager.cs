// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.HotReload;

[assembly: MetadataUpdateHandler(typeof(HotReloadManager))]

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal static class HotReloadManager
    {
        public static event Action? OnDeltaApplied;

        /// <summary>
        /// Gets a value that determines if OnDeltaApplied is subscribed to.
        /// </summary>
        public static bool IsSubscribedTo => OnDeltaApplied is not null;

        /// <summary>
        /// MetadataUpdateHandler event. This is invoked by the hot reload host via reflection.
        /// </summary>
        public static void UpdateApplication(Type[]? _) => OnDeltaApplied?.Invoke();
    }
}
