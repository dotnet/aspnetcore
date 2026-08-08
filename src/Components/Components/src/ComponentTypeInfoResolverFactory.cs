// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal static class ComponentTypeInfoResolverFactory
{
    internal static IComponentTypeInfoResolver Default { get; } = new ReflectionComponentTypeInfoResolver();

    internal static IComponentTypeInfoResolver Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new ReflectionComponentTypeInfoResolver();
    }
}
