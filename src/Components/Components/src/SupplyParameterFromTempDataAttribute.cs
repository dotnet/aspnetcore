// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that the value of the associated property should be supplied from
/// the TempData with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SupplyParameterFromTempDataAttribute : CascadingParameterAttributeBase
{
    /// <summary>
    /// Gets or sets the TempData key. If not specified, the property name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <inheritdoc />
    internal override bool SingleDelivery => false;
}
