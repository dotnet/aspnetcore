// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Describes a type capable of setting a property on a component.
/// </summary>
public interface IPropertySetter
{
    /// <summary>
    /// Gets whether the property is cascading.
    /// </summary>
    bool Cascading { get; }

    /// <summary>
    /// Sets the property on the <paramref name="target"/> with the provided <paramref name="value"/>.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="value"></param>
    void SetValue(object target, object value);
}

/// <summary>
/// Describes a type capable of setting the component property capturing unmatched values.
/// </summary>
public interface IUnmatchedValuesPropertySetter : IPropertySetter
{
    /// <summary>
    /// Gets the name of the component property that this instance sets.
    /// </summary>
    string UnmatchedValuesPropertyName { get; }
}
