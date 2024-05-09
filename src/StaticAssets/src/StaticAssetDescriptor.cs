// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

// Represents a static resource.
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class StaticAssetDescriptor(
    string route,
    string assetFile,
    StaticAssetSelector[] selectors,
    EndpointProperty[] endpointProperties,
    ResponseHeader[] responseHeaders)
{
    public string Route { get; } = route;
    public string AssetFile { get; } = assetFile;
    public StaticAssetSelector[] Selectors { get; } = selectors;
    public EndpointProperty[] EndpointProperties { get; } = endpointProperties;
    public ResponseHeader[] ResponseHeaders { get; } = responseHeaders;

    private string GetDebuggerDisplay()
    {
        return $"Route: {Route} Path:{AssetFile}";
    }
}
