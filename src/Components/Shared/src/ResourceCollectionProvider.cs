// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

using static Microsoft.AspNetCore.Internal.LinkerFlags;

internal class ResourceCollectionProvider
{
    private string? _url;

    [PersistentState]
    public string? ResourceCollectionUrl
    {
        get => _url;
        set
        {
            if (_url != null)
            {
                throw new InvalidOperationException("The resource collection URL has already been set.");
            }
            _url = value;
        }
    }

    private ResourceAssetCollection? _resourceCollection;
    private readonly IJSRuntime _jsRuntime;
    public ResourceCollectionProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
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

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", _url);
            var result = await module.InvokeAsync<ResourceAsset[]>("get");
            return result == null ? ResourceAssetCollection.Empty : new ResourceAssetCollection(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load the Blazor resource collection from '{_url}'. " +
                "This is likely caused by a mismatch in the file integrity check, which can happen when files are modified after they are published. " +
                "Ensure that all published files are deployed correctly and that none have been modified after publish.",
                ex);
        }
    }
}
