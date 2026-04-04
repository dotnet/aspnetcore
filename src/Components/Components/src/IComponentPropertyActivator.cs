// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides a mechanism for activating properties on Blazor component instances.
/// </summary>
/// <remarks>
/// This interface allows customization of how properties marked with <see cref="InjectAttribute"/>
/// are populated on component instances. The default implementation uses the <see cref="IServiceProvider"/>
/// to resolve services for injection.
/// </remarks>
public interface IComponentPropertyActivator
{
    /// <summary>
    /// Gets a delegate that activates properties on a component of the specified type.
    /// </summary>
    /// <param name="componentType">The type of component to create an activator for.</param>
    /// <returns>
    /// A delegate that takes an <see cref="IServiceProvider"/> and an <see cref="IComponent"/>
    /// instance, and populates the component's injectable properties.
    /// </returns>
    Action<IServiceProvider, IComponent> GetActivator(
        [DynamicallyAccessedMembers(Component)] Type componentType);
}
