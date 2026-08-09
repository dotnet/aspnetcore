// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal static class ComponentTypeInfoResolverFactory
{
    internal static IComponentTypeInfoResolver Default { get; } =
        new CompositeComponentTypeInfoResolver([new ReflectionComponentTypeInfoResolver()]);

    internal static IComponentTypeInfoResolver Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var resolvers = new List<IComponentTypeInfoResolver>();
        if (services.GetService<IComponentMetadataResolver>() is { } metadataResolver)
        {
            resolvers.Add(new SourceGeneratedComponentTypeInfoResolver(metadataResolver));
        }

        resolvers.Add(new ReflectionComponentTypeInfoResolver());

        return new CompositeComponentTypeInfoResolver(resolvers);
    }
}
