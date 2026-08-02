// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.Extensions.Primitives;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Components.Endpoints.HotReloadService))]

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HotReloadService : IDisposable
{
    public HotReloadService()
    {
        UpdateApplicationEvent += NotifyUpdateApplication;
        MetadataUpdateSupported = MetadataUpdater.IsSupported;
    }

    private CancellationTokenSource _tokenSource = new();
    private static event Action<Type[]?>? UpdateApplicationEvent;
    internal static event Action<Type[]?>? ClearCacheEvent;

    public bool MetadataUpdateSupported { get; internal set; }

    public IChangeToken GetChangeToken() => new CancellationChangeToken(_tokenSource.Token);

    public static void UpdateApplication(Type[]? changedTypes)
    {
        UpdateApplicationEvent?.Invoke(changedTypes);
    }
    
    public static void ClearCache(Type[]? types) 
    { 
        ClearCacheEvent?.Invoke(types);
    }

    private void NotifyUpdateApplication(Type[]? changedTypes)
    {
        var current = Interlocked.Exchange(ref _tokenSource, new CancellationTokenSource());
        current.Cancel();
        current.Dispose();
    }

    public void Dispose()
    {
        UpdateApplicationEvent -= NotifyUpdateApplication;
        _tokenSource.Dispose();
    }
}
