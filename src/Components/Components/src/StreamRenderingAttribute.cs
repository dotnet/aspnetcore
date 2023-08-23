// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// An attribute to indicate whether to stream the rendering of a component and its descendants.
/// 
/// This attribute only takes effect within renderers that support streaming rendering (for example,
/// server-side HTML rendering from a Razor Component endpoint). In other hosting models it has no effect.
///
/// If a component type does not declare this attribute, then instances of that component type will share
/// the same streaming rendering mode as their parent component.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class StreamRenderingAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="StreamRenderingAttribute"/>
    /// </summary>
    /// <param name="enabled">A flag to indicate whether this component and its descendants should stream their rendering. The default value is true.</param>
    public StreamRenderingAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }

    /// <summary>
    /// Gets a flag indicating whether this component and its descendants should stream their rendering.
    /// </summary>
    public bool Enabled { get; }
}
