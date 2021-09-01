// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Describes a type capable of providing <see cref="IPropertySetter"/> instances for its properties.
/// </summary>
public interface IPropertySetterProvider
{
    /// <summary>
    /// The property setter that sets the component property capturing unmatched values.
    /// </summary>
    IUnmatchedValuesPropertySetter? UnmatchedValuesPropertySetter { get; }

    /// <summary>
    /// Gets the setter associated with the provided <paramref name="propertyName"/>, returning
    /// <c>true</c> if it exists, otherwise <c>false</c>.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="propertySetter">The result <see cref="IPropertySetter"/>, if it exists.</param>
    /// <returns></returns>
    bool TryGetSetter(string propertyName, [NotNullWhen(returnValue: true)] out IPropertySetter? propertySetter);
}
