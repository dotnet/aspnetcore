// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;

namespace Microsoft.AspNetCore.Components
{
    /// <remarks>
    /// The <see cref="Instance"/> property on this type is used as a static global cache. Ensure any changes to this type
    /// are thread safe and can be safely cached statically.
    /// </remarks>
    internal class ComponentFactory
    {
        private static readonly BindingFlags _injectablePropertyBindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _cachedInitializers
            = new ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>();

        public static readonly ComponentFactory Instance = new ComponentFactory();

        public IComponent InstantiateComponent(IServiceProvider serviceProvider, Type componentType)
        {
            var instance = Activator.CreateInstance(componentType);
            if (!(instance is IComponent component))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            PerformPropertyInjection(serviceProvider, component);
            return component;
        }

        private void PerformPropertyInjection(IServiceProvider serviceProvider, IComponent instance)
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

        private Action<IServiceProvider, IComponent> CreateInitializer(Type type)
        {
            // Do all the reflection up front
            var injectableProperties =
                MemberAssignment.GetPropertiesIncludingInherited(type, _injectablePropertyBindingFlags)
                .Where(p => p.IsDefined(typeof(InjectAttribute)));

            var injectables = injectableProperties.Select(property =>
            (
                propertyName: property.Name,
                propertyType: property.PropertyType,
                setter: MemberAssignment.CreatePropertySetter(type, property, cascading: false)
            )).ToArray();

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
}
