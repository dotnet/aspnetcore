// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    internal class DefaultComponentActivator : IComponentActivator
    {
        public static IComponentActivator Instance { get; } = new DefaultComponentActivator();

        /// <inheritdoc />
        public IComponent CreateInstance(Type componentType)
        {
            var instance = Activator.CreateInstance(componentType);
            if (!(instance is IComponent component))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            return component;
        }
    }
}
