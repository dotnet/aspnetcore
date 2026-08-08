// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class ComponentJsonMetadataResolver : IComponentJsonMetadataResolver
{
    public ComponentJsonMetadataResolver(IOptions<ComponentJsonMetadataOptions> options)
    {
        var resolvers = options.Value.Resolvers;
        JsonTypeInfoResolver = resolvers.Count switch
        {
            0 => null,
            1 => resolvers[0],
            _ => System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine([.. resolvers]),
        };
    }

    public IJsonTypeInfoResolver? JsonTypeInfoResolver { get; }
}
