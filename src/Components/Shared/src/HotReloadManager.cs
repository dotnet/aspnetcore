// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.HotReload;

[assembly: MetadataUpdateHandler(typeof(HotReloadManager))]

namespace Microsoft.AspNetCore.Components.HotReload;

internal sealed class HotReloadManager
{
    public static readonly HotReloadManager Default = new();

    public bool IsEnabled { get; set; } = IsSupported;

    [FeatureSwitchDefinition("System.Reflection.Metadata.MetadataUpdater.IsSupported")]
    internal static bool IsSupported { get; } =
        AppContext.TryGetSwitch("System.Reflection.Metadata.MetadataUpdater.IsSupported", out bool isSupported) ? isSupported : true;

    /// <summary>
    /// Gets a value that determines if OnDeltaApplied is subscribed to.
    /// </summary>
    public bool IsSubscribedTo => OnDeltaApplied is not null;

    public event Action? OnDeltaApplied;

    /// <summary>
    /// MetadataUpdateHandler event. This is invoked by the hot reload host via reflection.
    /// </summary>
    public static void UpdateApplication(Type[]? _) => Default.OnDeltaApplied?.Invoke();

    // For testing purposes only
    internal void TriggerOnDeltaApplied() => OnDeltaApplied?.Invoke();
}
