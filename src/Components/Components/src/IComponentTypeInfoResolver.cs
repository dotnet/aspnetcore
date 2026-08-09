// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components;

internal interface IComponentTypeInfoResolver
{
    ComponentTypeInfo? GetTypeInfo(Type componentType);

    ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName);

    IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly);
}

internal static class ComponentTypeInfoResolverExtensions
{
    public static IReadOnlyList<ComponentTypeInfo> GetRequiredTypeInfos(
        this IComponentTypeInfoResolver resolver,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(assembly);

        var typeInfos = resolver.GetTypeInfos(assembly);
        if (typeInfos.Count == 0 && !ComponentMetadataFeature.IsReflectionEnabledByDefault)
        {
            throw new NotSupportedException(
                $"Component metadata for assembly '{assembly.GetName().Name}' could not be resolved. " +
                "Register generated component metadata for every explicitly requested component assembly.");
        }

        return typeInfos;
    }

    public static ComponentTypeInfo GetRequiredTypeInfo(
        this IComponentTypeInfoResolver resolver,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(componentType);

        return resolver.GetTypeInfo(componentType)
            ?? throw new NotSupportedException(
                $"Component metadata for type '{componentType.FullName}' could not be resolved.");
    }

    public static ComponentTypeInfo GetRequiredTypeInfo(
        this IComponentTypeInfoResolver resolver,
        string assemblyName,
        string typeName)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(typeName);

        return resolver.GetTypeInfo(assemblyName, typeName)
            ?? throw new NotSupportedException(
                $"Component metadata for type '{typeName}' in assembly '{assemblyName}' could not be resolved.");
    }
}
