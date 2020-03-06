// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents an activator that can be used to instantiate components.
    /// </summary>
    public interface IComponentActivator
    {
        /// <summary>
        /// Creates an component of the specified type using that type's default constructor.
        /// </summary>
        /// <param name="componentType">The type of component to create.</param>
        /// <returns>A reference to the newly created component.</returns>
        object CreateInstance(Type componentType);
    }
}
