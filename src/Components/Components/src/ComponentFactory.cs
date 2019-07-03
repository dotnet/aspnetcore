// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;

namespace Microsoft.AspNetCore.Components
{
    internal class ComponentFactory
    {
        private readonly static BindingFlags _injectablePropertyBindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly IDictionary<Type, Action<IServiceProvider, IComponent>> _cachedInitializers
            = new ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>();

        public IComponent InstantiateComponent(IServiceProvider serviceProvider, Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not " +
                    $"implement {nameof(IComponent)}.", nameof(componentType));
            }

            var instance = (IComponent)Activator.CreateInstance(componentType);
            PerformPropertyInjection(serviceProvider, instance);
            return instance;
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
                _cachedInitializers[instanceType] = initializer;
            }

            initializer(serviceProvider, instance);
        }

        private static Action<IServiceProvider, IComponent> CreateInitializer(Type type)
        {
            // Do all the reflection up front
            var injectableProperties =
                MemberAssignment.GetPropertiesIncludingInherited(type, _injectablePropertyBindingFlags)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);
            var injectables = injectableProperties.Select(property =>
            (
                propertyName: property.Name,
                propertyType: property.PropertyType,
                setter: MemberAssignment.CreatePropertySetter(type, property)
            )).ToArray();

            // Return an action whose closure can write all the injected properties
            // without any further reflection calls (just typecasts)
            return (serviceProvider, instance) =>
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

                    setter.SetValue(instance, serviceInstance);
                }
            };
        }
    }
}
