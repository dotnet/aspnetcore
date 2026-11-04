// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Contains information about a cascading parameter.
/// </summary>
public readonly struct CascadingParameterInfo
{
    /// <summary>
    /// Gets the property's <see cref="CascadingParameterAttributeBase"/> attribute.
    /// </summary>
    public CascadingParameterAttributeBase Attribute { get; }

    /// <summary>
    /// Gets the name of the parameter's property.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the type of the parameter's property.
    /// </summary>
    public Type PropertyType { get; }

    internal CascadingParameterInfo(CascadingParameterAttributeBase attribute, string propertyName, Type propertyType)
    {
        Attribute = attribute;
        PropertyName = propertyName;
        PropertyType = propertyType;
    }
}
