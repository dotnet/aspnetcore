// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates associated property or all properties with the associated type should be edited using an
/// &lt;input&gt; element of type "hidden".
/// </summary>
/// <remarks>
/// When overriding a <see cref="HiddenInputAttribute"/> inherited from a base class, should apply both
/// <c>[HiddenInput(DisplayValue = true)]</c> (if the inherited attribute had <c>DisplayValue = false</c>) and a
/// <see cref="System.ComponentModel.DataAnnotations.UIHintAttribute"/> with some value other than "HiddenInput".
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HiddenInputAttribute : Attribute
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="HiddenInputAttribute"/> class.
    /// </summary>
    public HiddenInputAttribute()
    {
        DisplayValue = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to display the value as well as provide a hidden &lt;input&gt;
    /// element. The default value is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// If <c>false</c>, also causes the default <see cref="object"/> display and editor templates to return HTML
    /// lacking the usual per-property &lt;div&gt; wrapper around the associated property and the default display
    /// "HiddenInput" template to return <c>string.Empty</c> for the associated property. Thus the default
    /// <see cref="object"/> display template effectively skips the property and the default <see cref="object"/>
    /// editor template returns only the hidden &lt;input&gt; element for the property.
    /// </remarks>
    public bool DisplayValue { get; set; }
}
