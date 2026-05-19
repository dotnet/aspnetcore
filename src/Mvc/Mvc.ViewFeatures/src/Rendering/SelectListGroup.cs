// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Represents the optgroup HTML element and its attributes.
/// In a select list, multiple groups with the same name are supported.
/// They are compared with reference equality.
/// </summary>
public class SelectListGroup
{
    /// <summary>
    /// Gets or sets a value that indicates whether this <see cref="SelectListGroup"/> is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Represents the value of the optgroup's label.
    /// </summary>
    public string Name { get; set; }
}
