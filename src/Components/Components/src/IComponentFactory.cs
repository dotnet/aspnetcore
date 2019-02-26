// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a factory that can be used to instantiate components.
    /// </summary>
    public interface IComponentFactory
    {
        /// <summary>
        /// Instantiates a component of the specified type.
        /// </summary>
        /// <param name="componentType">The component type.</param>
        /// <returns>The instantiated component of type <paramref name="componentType"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="componentType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when instances of type <paramref name="componentType"/> are not assignable to <see cref="IComponent"/>.</exception>
        IComponent InstantiateComponent(Type componentType);
    }
}
