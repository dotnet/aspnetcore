// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.StaticAssets;

// Represents a manifest of static resources.
// The manifes is a JSON file that contains a list of static resources and their associated metadata.
// There is a top level property "resources" that contains an array of objects, each of which represents a static resource.
// Each static  resource is defined by the following properties:
// * "path": The path of the static resource.
// * "selectors": An array of request headers that act as selectors for the resource. Each selector is defined by an object with the following properties:
//   * "name": The name of the request header.
//   * "value": The value of the request header.
//   * "preference": The preference of the selector. The preference is a number between 0 and 1.0 and it matches the semantics of the quality parameter in
//     the Accept-* headers. This preference is used as a last resource to break ties in content negotiation when the client indicates an equal preference
//     for multiple resources.
// * "responseHeaders": A list of headers to apply to the response when a given resource is served. This is useful to apply headers to the response that are
//   specific to the resource, such as Cache-Control headers or ETag headers that are computed at build time.
internal class StaticAssetsManifest
{
    internal static StaticAssetsManifest Parse(string manifestPath)
    {
        ArgumentNullException.ThrowIfNull(manifestPath);

        using var stream = File.OpenRead(manifestPath);
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var result = JsonSerializer.Deserialize(content, StaticAssetsManifestJsonContext.Default.StaticAssetsManifest) ??
            throw new InvalidOperationException($"The static resources manifest file '{manifestPath}' could not be deserialized.");

        return result;
    }

    internal static StaticAssetsEndpointDataSource CreateDataSource(IEndpointRouteBuilder endpoints, string manifestName, List<StaticAssetDescriptor> descriptors)
    {
        var dataSource = new StaticAssetsEndpointDataSource(endpoints.ServiceProvider, new StaticAssetEndpointFactory(endpoints.ServiceProvider), manifestName, descriptors);
        endpoints.DataSources.Add(dataSource);
        return dataSource;
    }

    public int Version { get; set; }

    public List<StaticAssetDescriptor> Endpoints { get; set; } = [];
}
