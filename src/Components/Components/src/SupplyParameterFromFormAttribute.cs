// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that routing components may supply a value for the parameter from the
/// current form post, if any. This is only applicable during passive server-side rendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SupplyParameterFromFormAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the form parameter. If null, the form
    /// parameter is assumed to have the same name as the associated property.
    /// </summary>
    public string? Name { get; set; }
}
