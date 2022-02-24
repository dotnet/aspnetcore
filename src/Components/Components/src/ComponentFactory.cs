// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentFactory
{
    private const BindingFlags _injectablePropertyBindingFlags
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly ConcurrentDictionary<Type, ComponentInitializer> _cachedInitializers = new();
    private readonly IComponentActivator? _componentActivator;

    public ComponentFactory(IComponentActivator? componentActivator)
    {
        _componentActivator = componentActivator;
    }

    public static void ClearCache() => _cachedInitializers.Clear();

    public IComponent InstantiateComponent(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (_componentActivator is not null)
        {
            return InstantiateWithActivator(_componentActivator, serviceProvider, componentType);
        }

        return InstantiateDefault(serviceProvider, componentType);
    }

    private static IComponent InstantiateDefault(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        // This is thread-safe because _cachedInitializers is a ConcurrentDictionary.
        // We might generate the initializer more than once for a given type, but would
        // still produce the correct result.
        if (!_cachedInitializers.TryGetValue(componentType, out var initializer))
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            initializer = new(CreatePropertyInitializer(componentType), ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes));
            _cachedInitializers.TryAdd(componentType, initializer);
        }

        return initializer.CreateDefault(serviceProvider);
    }

    private static IComponent InstantiateWithActivator(IComponentActivator componentActivator, IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        var component = componentActivator.CreateInstance(componentType);
        if (component is null)
        {
            // A user implemented IComponentActivator might return null.
            throw new InvalidOperationException($"The component activator returned a null value for a component of type {componentType.FullName}.");
        }

        // Use the activated type instead of specified type since the activator may return different/ derived instances.
        componentType = component.GetType();

        // This is thread-safe because _cachedInitializers is a ConcurrentDictionary.
        // We might generate the initializer more than once for a given type, but would
        // still produce the correct result.
        if (!_cachedInitializers.TryGetValue(componentType, out var initializer))
        {
            initializer = new(CreatePropertyInitializer(componentType));
            _cachedInitializers.TryAdd(componentType, initializer);
        }

        initializer.ActivateProperties(serviceProvider, component);
        return component;
    }

    private static Action<IServiceProvider, IComponent> CreatePropertyInitializer([DynamicallyAccessedMembers(Component)] Type type)
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

    private readonly struct ComponentInitializer
    {
        private readonly Action<IServiceProvider, IComponent> _propertyInitializer;

        private readonly ObjectFactory? _componentFactory;

        public ComponentInitializer(Action<IServiceProvider, IComponent> propertyInitializer, ObjectFactory? componentFactory = null)
        {
            _propertyInitializer = propertyInitializer;
            _componentFactory = componentFactory;
        }

        public IComponent CreateDefault(IServiceProvider serviceProvider)
        {
            var component = (IComponent)_componentFactory!(serviceProvider, Array.Empty<object?>());
            ActivateProperties(serviceProvider, component);
            return component;
        }

        public void ActivateProperties(IServiceProvider serviceProvider, IComponent component)
        {
            _propertyInitializer(serviceProvider, component);
        }
    }
}
