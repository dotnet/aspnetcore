// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components
{
    internal class DefaultComponentActivator : IComponentActivator
    {
        public static IComponentActivator Instance { get; } = new DefaultComponentActivator();

        /// <inheritdoc />
        public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            return (IComponent)Activator.CreateInstance(componentType)!;
        }
    }
}
