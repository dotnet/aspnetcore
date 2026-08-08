// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;

namespace Microsoft.AspNetCore.Components;

internal sealed class CompositeComponentTypeInfoResolver : IComponentTypeInfoResolver, IDisposable
{
    private readonly IComponentTypeInfoResolver[] _resolvers;
    private readonly SourceGeneratedComponentTypeInfoResolver? _sourceGeneratedResolver;
    private readonly ReflectionComponentTypeInfoResolver? _reflectionResolver;
    private readonly object _cacheLock = new();
    private readonly ConcurrentDictionary<Type, ComponentTypeInfo> _typeInfoCache = new();
    private readonly ConcurrentDictionary<(string AssemblyName, string TypeName), ComponentTypeInfo> _nameCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<ComponentTypeInfo>> _assemblyCache = new();

    internal CompositeComponentTypeInfoResolver(IReadOnlyList<IComponentTypeInfoResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        _resolvers = [.. resolvers];
        _sourceGeneratedResolver = _resolvers.OfType<SourceGeneratedComponentTypeInfoResolver>().FirstOrDefault();
        _reflectionResolver = _resolvers.OfType<ReflectionComponentTypeInfoResolver>().FirstOrDefault();

        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += OnDeltaApplied;
        }
    }

    public ComponentTypeInfo? GetTypeInfo(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!_typeInfoCache.TryGetValue(componentType, out var typeInfo))
        {
            lock (_cacheLock)
            {
                if (!_typeInfoCache.TryGetValue(componentType, out typeInfo))
                {
                    typeInfo = ResolveTypeInfo(componentType);
                    if (typeInfo is not null)
                    {
                        _typeInfoCache[componentType] = typeInfo;
                    }
                }
            }
        }

        return typeInfo;
    }

    public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(typeName);

        var key = (assemblyName, typeName);
        if (!_nameCache.TryGetValue(key, out var typeInfo))
        {
            lock (_cacheLock)
            {
                if (!_nameCache.TryGetValue(key, out typeInfo))
                {
                    typeInfo = ResolveTypeInfo(assemblyName, typeName);
                    if (typeInfo is not null)
                    {
                        typeInfo = _typeInfoCache.GetOrAdd(typeInfo.Type, typeInfo);
                        _nameCache[key] = typeInfo;
                    }

                }
            }
        }

        return typeInfo;
    }

    public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblyCache.TryGetValue(assembly, out var typeInfos))
        {
            lock (_cacheLock)
            {
                if (!_assemblyCache.TryGetValue(assembly, out typeInfos))
                {
                    var resolved = new List<ComponentTypeInfo>();
                    var indexes = new Dictionary<Type, int>();

                    foreach (var resolver in _resolvers)
                    {
                        foreach (var candidate in resolver.GetTypeInfos(assembly))
                        {
                            if (indexes.TryGetValue(candidate.Type, out var index))
                            {
                                if (resolved[index].CreateInstance is null && candidate.CreateInstance is not null)
                                {
                                    resolved[index] = resolved[index].WithCreateInstance(candidate.CreateInstance);
                                }

                                continue;
                            }

                            indexes.Add(candidate.Type, resolved.Count);
                            resolved.Add(candidate);
                        }
                    }

                    for (var i = 0; i < resolved.Count; i++)
                    {
                        resolved[i] = _typeInfoCache.GetOrAdd(resolved[i].Type, resolved[i]);
                    }

                    typeInfos = [.. resolved];
                    _assemblyCache[assembly] = typeInfos;
                }
            }
        }

        return typeInfos;
    }

    internal void ClearCaches()
    {
        lock (_cacheLock)
        {
            ClearCachesCore();
        }
    }

    private void ClearCachesCore()
    {
        _typeInfoCache.Clear();
        _nameCache.Clear();
        _assemblyCache.Clear();
        _sourceGeneratedResolver?.ClearCaches();
        _reflectionResolver?.ClearCaches();
    }

    public void Dispose()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied -= OnDeltaApplied;
        }

        foreach (var disposable in _resolvers.OfType<IDisposable>())
        {
            disposable.Dispose();
        }
    }

    private void OnDeltaApplied()
    {
        lock (_cacheLock)
        {
            if (_sourceGeneratedResolver is not null && _reflectionResolver is not null)
            {
                _sourceGeneratedResolver.Disable();
            }

            ClearCachesCore();
        }
    }

    private ComponentTypeInfo? ResolveTypeInfo(Type componentType)
    {
        ComponentTypeInfo? result = null;

        foreach (var resolver in _resolvers)
        {
            var candidate = resolver.GetTypeInfo(componentType);
            if (candidate is null)
            {
                continue;
            }

            result ??= candidate;

            if (result.CreateInstance is null && candidate.CreateInstance is not null && !ReferenceEquals(result, candidate))
            {
                result = result.WithCreateInstance(candidate.CreateInstance);
                break;
            }

            if (result.CreateInstance is not null)
            {
                break;
            }
        }

        return result;
    }

    private ComponentTypeInfo? ResolveTypeInfo(string assemblyName, string typeName)
    {
        ComponentTypeInfo? result = null;

        foreach (var resolver in _resolvers)
        {
            var candidate = resolver.GetTypeInfo(assemblyName, typeName);
            if (candidate is null)
            {
                continue;
            }

            result ??= candidate;

            if (result.CreateInstance is null && candidate.CreateInstance is not null && !ReferenceEquals(result, candidate))
            {
                result = result.WithCreateInstance(candidate.CreateInstance);
                break;
            }

            if (result.CreateInstance is not null)
            {
                break;
            }
        }

        return result;
    }
}
