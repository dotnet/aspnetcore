// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

internal sealed class SourceGeneratedComponentTypeInfoResolver : IComponentTypeInfoResolver
{
    private readonly Dictionary<Type, ComponentDescriptor> _descriptorsByType;
    private readonly Dictionary<(string AssemblyName, string TypeName), ComponentDescriptor> _descriptorsByName;
    private readonly Type[] _orderedComponentTypes;
    private readonly ConcurrentDictionary<Type, ComponentTypeInfo> _typeInfoCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<ComponentTypeInfo>> _assemblyCache = new();
    private volatile bool _enabled = true;

    internal SourceGeneratedComponentTypeInfoResolver(IComponentMetadataResolver metadataResolver)
    {
        ArgumentNullException.ThrowIfNull(metadataResolver);

        _descriptorsByType = [];
        _descriptorsByName = [];

        var seenTypes = new HashSet<Type>();
        var orderedTypes = new List<Type>();

        foreach (var descriptor in metadataResolver.Components)
        {
            var effectiveDescriptor = _descriptorsByType.TryGetValue(descriptor.Type, out var existingDescriptor)
                ? MergeDescriptors(existingDescriptor, descriptor)
                : descriptor;
            _descriptorsByType[descriptor.Type] = effectiveDescriptor;

            if (seenTypes.Add(descriptor.Type))
            {
                orderedTypes.Add(descriptor.Type);
            }

            if (descriptor.Type.Assembly.GetName().Name is { } assemblyName &&
                descriptor.Type.FullName is { } typeName)
            {
                _descriptorsByName[(assemblyName, typeName)] = effectiveDescriptor;
            }
        }

        _orderedComponentTypes = [.. orderedTypes];
    }

    private static ComponentDescriptor MergeDescriptors(
        ComponentDescriptor generated,
        ComponentDescriptor builtIn)
        => new()
        {
            Type = generated.Type,
            CreateInstance = builtIn.CreateInstance ?? generated.CreateInstance,
            Parameters = MergeMembers(generated.Parameters, builtIn.Parameters, static parameter => parameter.Name),
            Injectables = MergeMembers(generated.Injectables, builtIn.Injectables, static injectable => injectable.Name),
            Metadata = MergeMetadata(generated.Metadata, builtIn.Metadata),
        };

    private static IReadOnlyList<object> MergeMetadata(
        IReadOnlyList<object> generated,
        IReadOnlyList<object> builtIn)
    {
        if (builtIn.Count == 0)
        {
            return generated;
        }

        var merged = new List<object>(generated);
        foreach (var metadata in builtIn)
        {
            if (!merged.Contains(metadata))
            {
                merged.Add(metadata);
            }
        }

        return merged;
    }

    private static IReadOnlyList<T> MergeMembers<T>(
        IReadOnlyList<T> generated,
        IReadOnlyList<T> builtIn,
        Func<T, string> getName)
    {
        if (builtIn.Count == 0)
        {
            return generated;
        }

        if (generated.Count == 0)
        {
            return builtIn;
        }

        var merged = new List<T>(generated);
        var indexesByName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < generated.Count; i++)
        {
            indexesByName[getName(generated[i])] = i;
        }

        foreach (var member in builtIn)
        {
            var name = getName(member);
            if (!indexesByName.ContainsKey(name))
            {
                indexesByName[name] = merged.Count;
                merged.Add(member);
            }
        }

        return merged;
    }

    public ComponentTypeInfo? GetTypeInfo(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!_enabled || !_descriptorsByType.TryGetValue(componentType, out var descriptor))
        {
            return null;
        }

        if (!_typeInfoCache.TryGetValue(componentType, out var typeInfo))
        {
            typeInfo = new ComponentTypeInfo(descriptor);
            _typeInfoCache.TryAdd(componentType, typeInfo);
        }

        return typeInfo;
    }

    public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(typeName);

        if (!_enabled || !_descriptorsByName.TryGetValue((assemblyName, typeName), out var descriptor))
        {
            return null;
        }

        return GetTypeInfo(descriptor.Type);
    }

    public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_enabled)
        {
            return [];
        }

        if (!_assemblyCache.TryGetValue(assembly, out var typeInfos))
        {
            var results = new List<ComponentTypeInfo>();

            foreach (var componentType in _orderedComponentTypes)
            {
                if (componentType.Assembly == assembly && GetTypeInfo(componentType) is { } typeInfo)
                {
                    results.Add(typeInfo);
                }
            }

            typeInfos = [.. results];
            _assemblyCache.TryAdd(assembly, typeInfos);
        }

        return typeInfos;
    }

    internal void ClearCaches()
    {
        _typeInfoCache.Clear();
        _assemblyCache.Clear();
    }

    internal void Disable()
    {
        _enabled = false;
        ClearCaches();
    }
}
