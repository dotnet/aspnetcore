// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Components
{
    internal class ComponentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly static BindingFlags _injectablePropertyBindingFlags
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
            // TODO: Cache delegates, etc
            var type = instance.GetType();
            var properties = type.GetTypeInfo().GetProperties(_injectablePropertyBindingFlags);
            foreach (var property in properties)
            {
                var injectAttribute = property.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null)
                {
                    var serviceInstance = _serviceProvider.GetService(property.PropertyType);
                    if (serviceInstance == null)
                    {
                        throw new InvalidOperationException($"Cannot provide value for property " +
                            $"'{property.Name}' on type '{type.FullName}'. There is no registered " +
                            $"service of type '{property.PropertyType.FullName}'.");
                    }

                    property.SetValue(instance, serviceInstance);
                }
            }
        }
    }
}
