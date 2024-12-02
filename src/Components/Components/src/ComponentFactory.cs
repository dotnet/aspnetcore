// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentFactory
{
    private const BindingFlags _injectablePropertyBindingFlags
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly ConcurrentDictionary<Type, ComponentTypeInfoCacheEntry> _cachedComponentTypeInfo = new();

    private readonly IComponentActivator _componentActivator;
    private readonly Renderer _renderer;

    public ComponentFactory(IComponentActivator componentActivator, Renderer renderer)
    {
        _componentActivator = componentActivator ?? throw new ArgumentNullException(nameof(componentActivator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public static void ClearCache() => _cachedComponentTypeInfo.Clear();

    private static ComponentTypeInfoCacheEntry GetComponentTypeInfo([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        // Unfortunately we can't use 'GetOrAdd' here because the DynamicallyAccessedMembers annotation doesn't flow through to the
        // callback, so it becomes an IL2111 warning. The following is equivalent and thread-safe because it's a ConcurrentDictionary
        // and it doesn't matter if we build a cache entry more than once.
        if (!_cachedComponentTypeInfo.TryGetValue(componentType, out var cacheEntry))
        {
            var componentTypeRenderMode = componentType.GetCustomAttribute<RenderModeAttribute>()?.Mode;
            cacheEntry = new ComponentTypeInfoCacheEntry(
                componentTypeRenderMode,
                CreatePropertyInjector(componentType));
            _cachedComponentTypeInfo.TryAdd(componentType, cacheEntry);
        }

        return cacheEntry;
    }

    public IComponent InstantiateComponent(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType, IComponentRenderMode? callerSpecifiedRenderMode, int? parentComponentId)
    {
        var (componentTypeRenderMode, propertyInjector) = GetComponentTypeInfo(componentType);
        IComponent component;

        if (componentTypeRenderMode is null && callerSpecifiedRenderMode is null)
        {
            // Typical case where no rendermode is specified in either location. We don't call ResolveComponentForRenderMode in this case.
            component = _componentActivator.CreateInstance(componentType);
        }
        else
        {
            // At least one rendermode is specified. We require that it's exactly one, and use ResolveComponentForRenderMode with it.
            var effectiveRenderMode = callerSpecifiedRenderMode is null
                ? componentTypeRenderMode!
                : componentTypeRenderMode is null
                    ? callerSpecifiedRenderMode
                    : throw new InvalidOperationException($"The component type '{componentType}' has a fixed rendermode of '{componentTypeRenderMode}', so it is not valid to specify any rendermode when using this component.");
            component = _renderer.ResolveComponentForRenderMode(componentType, parentComponentId, _componentActivator, effectiveRenderMode);
        }

        if (component is null)
        {
            // The default activator/resolver will never do this, but an externally-supplied one might
            throw new InvalidOperationException($"The component activator returned a null value for a component of type {componentType.FullName}.");
        }

        if (component.GetType() == componentType)
        {
            // Fast, common case: use the cached data we already looked up
            propertyInjector(serviceProvider, component);
        }
        else
        {
            // Uncommon case where the activator/resolver returned a different type. Needs an extra cache lookup.
            PerformPropertyInjection(serviceProvider, component);
        }

        return component;
    }

    private static void PerformPropertyInjection(IServiceProvider serviceProvider, IComponent instance)
    {
        // Suppressed with "pragma warning disable" so ILLink Roslyn Anayzer doesn't report the warning.
#pragma warning disable IL2072 // 'componentType' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'Microsoft.AspNetCore.Components.ComponentFactory.GetComponentTypeInfo(Type)'.
        var componentTypeInfo = GetComponentTypeInfo(instance.GetType());
#pragma warning restore IL2072 // 'componentType' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'Microsoft.AspNetCore.Components.ComponentFactory.GetComponentTypeInfo(Type)'.

        componentTypeInfo.PerformPropertyInjection(serviceProvider, instance);
    }

    private static Action<IServiceProvider, IComponent> CreatePropertyInjector([DynamicallyAccessedMembers(Component)] Type type)
    {
        // Do all the reflection up front
        List<(string name, Type propertyType, PropertySetter setter, object? serviceKey)>? injectables = null;
        foreach (var property in MemberAssignment.GetPropertiesIncludingInherited(type, _injectablePropertyBindingFlags))
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

    // Tracks information about a specific component type that ComponentFactory uses
    private sealed class ComponentTypeInfoCacheEntry
    {
        public IComponentRenderMode? ComponentTypeRenderMode { get; }

        public Action<IServiceProvider, IComponent> PerformPropertyInjection { get; }

        public ComponentTypeInfoCacheEntry(
            IComponentRenderMode? componentTypeRenderMode,
            Action<IServiceProvider, IComponent> performPropertyInjection)
        {
            ComponentTypeRenderMode = componentTypeRenderMode;
            PerformPropertyInjection = performPropertyInjection;
        }

        public void Deconstruct(
            out IComponentRenderMode? componentTypeRenderMode,
            out Action<IServiceProvider, IComponent> performPropertyInjection)
        {
            componentTypeRenderMode = ComponentTypeRenderMode;
            performPropertyInjection = PerformPropertyInjection;
        }
    }
}
