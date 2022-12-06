// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates how a component should be rendered.
/// </summary>
public class ComponentRenderMode
{
    /// <summary>
    /// Indicates that no render mode was specified, so the component should continue rendering
    /// in the same way as its parent.
    /// </summary>
    public static readonly ComponentRenderMode Unspecified = new ComponentRenderMode(0);

    /// <summary>
    /// Represents the render mode as a numeric value.
    /// </summary>
    public readonly byte NumericValue;

    /// <summary>
    /// Constructs an instance of <see cref="ComponentRenderMode"/>.
    /// </summary>
    /// <param name="numericValue">A unique numeric value.</param>
    protected ComponentRenderMode(byte numericValue)
        => NumericValue = numericValue;
}
