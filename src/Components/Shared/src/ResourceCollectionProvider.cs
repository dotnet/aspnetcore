// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

using static Microsoft.AspNetCore.Internal.LinkerFlags;

internal class ResourceCollectionProvider
{
    private const string ResourceCollectionUrlKey = "__ResourceCollectionUrl";
    private string? _url;
    private ResourceAssetCollection? _resourceCollection;
    private readonly PersistentComponentState _state;
    private readonly IJSRuntime _jsRuntime;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Strings are not trimmed")]
    public ResourceCollectionProvider(PersistentComponentState state, IJSRuntime jsRuntime)
    {
        _state = state;
        _jsRuntime = jsRuntime;
        _ = _state.TryTakeFromJson(ResourceCollectionUrlKey, out _url);
    }

    [MemberNotNull(nameof(_url))]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Strings are not trimmed")]
    internal void SetResourceCollectionUrl(string url)
    {
        if (_url != null)
        {
            throw new InvalidOperationException("The resource collection URL has already been set.");
        }
        _url = url;
        PersistingComponentStateSubscription registration = default;
        registration = _state.RegisterOnPersisting(() =>
        {
            _state.PersistAsJson(ResourceCollectionUrlKey, _url);
            registration.Dispose();
            return Task.CompletedTask;
        }, RenderMode.InteractiveWebAssembly);
    }

    internal async Task<ResourceAssetCollection> GetResourceCollection()
    {
        _resourceCollection = _resourceCollection ??= await LoadResourceCollection();
        return _resourceCollection;
    }

    internal void SetResourceCollection(ResourceAssetCollection resourceCollection)
    {
        _resourceCollection = resourceCollection;
    }

    [DynamicDependency(JsonSerialized, typeof(ResourceAsset))]
    [DynamicDependency(JsonSerialized, typeof(ResourceAssetProperty))]
    private async Task<ResourceAssetCollection> LoadResourceCollection()
    {
        if (_url == null)
        {
            return ResourceAssetCollection.Empty;
        }

        var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", _url);
        var result = await module.InvokeAsync<ResourceAsset[]>("get");
        return result == null ? ResourceAssetCollection.Empty : new ResourceAssetCollection(result);
    }
}
