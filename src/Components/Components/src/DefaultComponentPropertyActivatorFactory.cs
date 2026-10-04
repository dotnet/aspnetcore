// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Factory to create a default <see cref="IComponentPropertyActivator"/> instance.
/// This allows custom implementations of <see cref="IComponentPropertyActivator"/> to reuse the framework's default implementation
/// without exposing the default implementation type publicly.
/// </summary>
public static class DefaultComponentPropertyActivatorFactory
{
    /// <summary>
    /// Creates a new <see cref="IComponentPropertyActivator"/> instance.
    /// </summary>
    /// <returns>A new <see cref="IComponentPropertyActivator"/> instance.</returns>
    public static IComponentPropertyActivator Create() => new DefaultComponentPropertyActivator();
}
