// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class DefaultComponentPropertyActivator : IComponentPropertyActivator
{
    private const BindingFlags InjectablePropertyBindingFlags
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _cachedPropertyActivators = new();

    static DefaultComponentPropertyActivator()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    public static void ClearCache() => _cachedPropertyActivators.Clear();

    /// <inheritdoc />
    public Action<IServiceProvider, IComponent> GetActivator(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        // Unfortunately we can't use 'GetOrAdd' here because the DynamicallyAccessedMembers annotation doesn't flow through to the
        // callback, so it becomes an IL2111 warning. The following is equivalent and thread-safe because it's a ConcurrentDictionary
        // and it doesn't matter if we build a cache entry more than once.
        if (!_cachedPropertyActivators.TryGetValue(componentType, out var activator))
        {
            activator = CreatePropertyActivator(componentType);
            _cachedPropertyActivators.TryAdd(componentType, activator);
        }

        return activator;
    }

    private static Action<IServiceProvider, IComponent> CreatePropertyActivator(
        [DynamicallyAccessedMembers(Component)] Type type)
    {
        // Do all the reflection up front
        List<(string name, Type propertyType, PropertySetter setter, object? serviceKey)>? injectables = null;
        foreach (var property in MemberAssignment.GetPropertiesIncludingInherited(type, InjectablePropertyBindingFlags))
        {
            var injectAttribute = property.GetCustomAttribute<InjectAttribute>();
            if (injectAttribute is null)
            {
                continue;
            }

            injectables ??= new();
            injectables.Add((property.Name, property.PropertyType, new PropertySetter(type, property), injectAttribute.Key));
        }

        if (injectables is null)
        {
            return static (_, _) => { };
        }

        return Initialize;

        // Return an action whose closure can write all the injected properties
        // without any further reflection calls (just typecasts)
        void Initialize(IServiceProvider serviceProvider, IComponent component)
        {
            foreach (var (propertyName, propertyType, setter, serviceKey) in injectables)
            {
                object? serviceInstance;

                if (serviceKey is not null)
                {
                    if (serviceProvider is not IKeyedServiceProvider keyedServiceProvider)
                    {
                        throw new InvalidOperationException($"Cannot provide a value for property " +
                            $"'{propertyName}' on type '{type.FullName}'. The service provider " +
                            $"does not implement '{nameof(IKeyedServiceProvider)}' and therefore " +
                            $"cannot provide keyed services.");
                    }

                    serviceInstance = keyedServiceProvider.GetKeyedService(propertyType, serviceKey)
                        ?? throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{propertyName}' on type '{type.FullName}'. There is no " +
                        $"registered keyed service of type '{propertyType}' with key '{serviceKey}'.");
                }
                else
                {
                    serviceInstance = serviceProvider.GetService(propertyType)
                        ?? throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{propertyName}' on type '{type.FullName}'. There is no " +
                        $"registered service of type '{propertyType}'.");
                }

                setter.SetValue(component, serviceInstance);
            }
        }
    }
}
