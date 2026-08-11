// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal static class ComponentJsonMetadata
{
#pragma warning disable ASPNETCORE9004
    public static IJsonTypeInfoResolver? GetApplicationResolver(IServiceProvider? services)
    {
        if (services is null)
        {
            return null;
        }

        var resolvers = services
            .GetServices<RazorComponentsMetadataContext>()
            .Select(static context => context.JsonTypeInfoResolver)
            .OfType<IJsonTypeInfoResolver>()
            .ToArray();

        return resolvers.Length switch
        {
            0 => null,
            1 => resolvers[0],
            _ => JsonTypeInfoResolver.Combine(resolvers),
        };
    }
#pragma warning restore ASPNETCORE9004
}
