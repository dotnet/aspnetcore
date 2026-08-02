// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal static class ExtensionMethodsCache
{
    public static Dictionary<ThisAndExtensionMethod, PackageSourceAndNamespace> ConstructFromWellKnownTypes(WellKnownTypes wellKnownTypes)
    {
        return new()
        {
            {
                new(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_Extensions_DependencyInjection_IServiceCollection), "AddOpenApi"),
                new("Microsoft.AspNetCore.OpenApi", "Microsoft.Extensions.DependencyInjection")
            },
            {
                new(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Builder_WebApplication), "MapOpenApi"),
                new("Microsoft.AspNetCore.OpenApi", "Microsoft.AspNetCore.Builder")
            }
        };
    }
}
