// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents an activator that can be used to instantiate components.
/// The activator is not responsible for dependency injection, since the framework
/// performs dependency injection to the resulting instances separately.
/// </summary>
public interface IComponentActivator
{
    /// <summary>
    /// Creates a component of the specified type.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <returns>A reference to the newly created component.</returns>
    IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType);
}
