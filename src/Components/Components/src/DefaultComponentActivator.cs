// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

internal sealed class DefaultComponentActivator : IComponentActivator
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
