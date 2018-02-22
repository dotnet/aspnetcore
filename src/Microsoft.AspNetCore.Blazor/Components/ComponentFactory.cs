// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    internal class ComponentFactory
    {
        private readonly IServiceProvider _serviceProvider;

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
            // TODO
        }
    }
}
