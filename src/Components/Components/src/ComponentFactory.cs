// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentFactory
{
    private const BindingFlags _injectablePropertyBindingFlags
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _cachedInitializers = new();

    private readonly IComponentActivator _componentActivator;

    public ComponentFactory(IComponentActivator componentActivator)
    {
        _componentActivator = componentActivator ?? throw new ArgumentNullException(nameof(componentActivator));
    }

    public static void ClearCache() => _cachedInitializers.Clear();

    public IComponent InstantiateComponent(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        var component = _componentActivator.CreateInstance(componentType);
        if (component is null)
        {
            // The default activator will never do this, but an externally-supplied one might
            throw new InvalidOperationException($"The component activator returned a null value for a component of type {componentType.FullName}.");
        }

        PerformPropertyInjection(serviceProvider, component);
        return component;
    }

    private static void PerformPropertyInjection(IServiceProvider serviceProvider, IComponent instance)
    {
        // This is thread-safe because _cachedInitializers is a ConcurrentDictionary.
        // We might generate the initializer more than once for a given type, but would
        // still produce the correct result.
        var instanceType = instance.GetType();
        if (!_cachedInitializers.TryGetValue(instanceType, out var initializer))
        {
            initializer = CreateInitializer(instanceType);
            _cachedInitializers.TryAdd(instanceType, initializer);
        }

        initializer(serviceProvider, instance);
    }

    private static Action<IServiceProvider, IComponent> CreateInitializer([DynamicallyAccessedMembers(Component)] Type type)
    {
        // Do all the reflection up front
        List<(string name, Type propertyType, PropertySetter setter)>? injectables = null;
        foreach (var property in MemberAssignment.GetPropertiesIncludingInherited(type, _injectablePropertyBindingFlags))
        {
            if (!property.IsDefined(typeof(InjectAttribute)))
            {
                continue;
            }

            injectables ??= new();
            injectables.Add((property.Name, property.PropertyType, new PropertySetter(type, property)));
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
            foreach (var (propertyName, propertyType, setter) in injectables)
            {
                var serviceInstance = serviceProvider.GetService(propertyType);
                if (serviceInstance == null)
                {
                    throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{propertyName}' on type '{type.FullName}'. There is no " +
                        $"registered service of type '{propertyType}'.");
                }

                setter.SetValue(component, serviceInstance);
            }
        }
    }
}
