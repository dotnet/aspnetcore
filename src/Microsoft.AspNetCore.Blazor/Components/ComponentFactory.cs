// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Components
{
    internal class ComponentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly static BindingFlags _injectablePropertyBindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly IDictionary<Type, Action<IComponent>> _cachedInitializers
            = new Dictionary<Type, Action<IComponent>>();

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
            var injectableProperties = type.GetTypeInfo()
                .GetProperties(_injectablePropertyBindingFlags)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);
            var injectables = injectableProperties.Select(property =>
            {
                if (property.SetMethod == null)
                {
                    throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{property.Name}' on type '{type.FullName}' because the property " +
                        $"has no setter.");
                }

                return
                (
                    propertyName: property.Name,
                    propertyType: property.PropertyType,
                    setter: (IPropertySetter)Activator.CreateInstance(
                        typeof(PropertySetter<,>).MakeGenericType(type, property.PropertyType),
                        property.SetMethod)
                );
            }).ToArray();

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

        private interface IPropertySetter
        {
            void SetValue(object target, object value);
        }

        private class PropertySetter<TTarget, TValue> : IPropertySetter
        {
            private readonly Action<TTarget, TValue> _setterDelegate;

            public PropertySetter(MethodInfo setMethod)
            {
                _setterDelegate = (Action<TTarget, TValue>)Delegate.CreateDelegate(
                    typeof(Action<TTarget, TValue>), setMethod);
            }

            public void SetValue(object target, object value)
                => _setterDelegate((TTarget)target, (TValue)value);
        }
    }
}
