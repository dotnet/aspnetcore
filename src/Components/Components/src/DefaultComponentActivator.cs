// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Default implementation of <see cref="IComponentActivator"/>.
    /// </summary>
    public class DefaultComponentActivator : IComponentActivator
    {
        // If no IComponentActivator is supplied by DI, the renderer uses this instance.
        // It's internal because in the future, we might want to add per-scope state and then
        // it would no longer be applicable to have a shared instance.
        internal static IComponentActivator Instance { get; } = new DefaultComponentActivator();

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
