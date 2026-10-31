// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a single parameter supplied to an <see cref="IComponent"/>
/// by its parent in the render tree.
/// </summary>
public readonly struct ParameterValue
{
    internal ParameterValue(string name, object value, bool cascading)
    {
        Name = name;
        Value = value;
        Cascading = cascading;
    }

    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value being supplied for the parameter.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Gets a value to indicate whether the parameter is cascading, meaning that it
    /// was supplied by a <see cref="CascadingValue{T}"/>.
    /// </summary>
    public bool Cascading { get; }
}
