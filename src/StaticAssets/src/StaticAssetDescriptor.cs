// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// The description of a static asset that was generated during the build process.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class StaticAssetDescriptor
{
    bool _isFrozen;
    private string? _route;
    private string? _assetFile;
    private IReadOnlyList<StaticAssetSelector> _selectors = [];
    private IReadOnlyList<StaticAssetProperty> _endpointProperties = [];
    private IReadOnlyList<StaticAssetResponseHeader> _responseHeaders = [];

    /// <summary>
    /// The route that the asset is served from.
    /// </summary>
    public required string Route
    {
        get => _route ?? throw new InvalidOperationException("Route is required");
        set => _route = !_isFrozen ? value : throw new InvalidOperationException("StaticAssetDescriptor is frozen and doesn't accept further changes");
    }

    /// <summary>
    /// The path to the asset file from the wwwroot folder.
    /// </summary>
    [JsonPropertyName("AssetFile")]
    public required string AssetPath
    {
        get => _assetFile ?? throw new InvalidOperationException("AssetPath is required");
        set => _assetFile = !_isFrozen ? value : throw new InvalidOperationException("StaticAssetDescriptor is frozen and doesn't accept further changes");
    }

    /// <summary>
    /// A list of selectors that are used to discriminate between two or more assets with the same route.
    /// </summary>
    [JsonPropertyName("Selectors")]
    public IReadOnlyList<StaticAssetSelector> Selectors
    {
        get => _selectors;
        set => _selectors = !_isFrozen ? value : throw new InvalidOperationException("StaticAssetDescriptor is frozen and doesn't accept further changes");
    }

    /// <summary>
    /// A list of properties that are associated with the endpoint.
    /// </summary>
    [JsonPropertyName("EndpointProperties")]
    public IReadOnlyList<StaticAssetProperty> Properties
    {
        get => _endpointProperties;
        set => _endpointProperties = !_isFrozen ? value : throw new InvalidOperationException("StaticAssetDescriptor is frozen and doesn't accept further changes");
    }

    /// <summary>
    /// A list of headers to apply to the response when this resource is served.
    /// </summary>
    [JsonPropertyName("ResponseHeaders")]
    public IReadOnlyList<StaticAssetResponseHeader> ResponseHeaders
    {
        get => _responseHeaders;
        set => _responseHeaders = !_isFrozen ? value : throw new InvalidOperationException("StaticAssetDescriptor is frozen and doesn't accept further changes");
    }

    private string GetDebuggerDisplay()
    {
        return $"Route: {Route} Path: {AssetPath}";
    }

    internal void Freeze()
    {
        _isFrozen = true;
    }
}
