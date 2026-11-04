// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies a fixed rendering mode for a component type.
///
/// Where possible, components should not specify any render mode this way, and should
/// be implemented to work across all render modes. Component authors should only specify
/// a fixed rendering mode when the component is incapable of running in other modes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class RenderModeAttribute : Attribute
{
    /// <summary>
    /// Gets the fixed rendering mode for a component type.
    /// </summary>
    public abstract IComponentRenderMode Mode { get; }
}
