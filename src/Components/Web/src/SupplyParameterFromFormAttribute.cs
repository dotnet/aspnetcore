// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that the value of the associated property should be supplied from
/// the form data for the form with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SupplyParameterFromFormAttribute : CascadingParameterAttributeBase
{
    /// <summary>
    /// Gets or sets the name of the form value. If not specified, the property name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the name of the form that provides this value. If not specified,
    /// the value will be mapped from any incoming form post within the current form
    /// mapping scope. If specified, the value will only be mapped from a form with
    /// the specified name in the current mapping scope.
    /// </summary>
    public string? FormName { get; set; }

    /// <inheritdoc />
    internal override bool SingleDelivery => true;
}
