// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    internal class ComponentFactory
    {
        private readonly static BindingFlags _injectablePropertyBindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<Type, Action<IComponent>> _cachedInitializers
            = new ConcurrentDictionary<Type, Action<IComponent>>();

        public ComponentFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IComponent InstantiateComponent(Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not " +
                    $"implement {nameof(IComponent)}.", nameof(componentType));
            }

            var instance = (IComponent)Activator.CreateInstance(componentType);
            PerformPropertyInjection(instance);
            return instance;
        }

        private void PerformPropertyInjection(IComponent instance)
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

            initializer(instance);
        }

        private Action<IComponent> CreateInitializer(Type type)
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
            return instance =>
            {
                foreach (var injectable in injectables)
                {
                    var serviceInstance = _serviceProvider.GetService(injectable.propertyType);
                    if (serviceInstance == null)
                    {
                        throw new InvalidOperationException($"Cannot provide a value for property " +
                            $"'{injectable.propertyName}' on type '{type.FullName}'. There is no " +
                            $"registered service of type '{injectable.propertyType}'.");
                    }

                    injectable.setter.SetValue(instance, serviceInstance);
                }
            };
        }
    }
}
