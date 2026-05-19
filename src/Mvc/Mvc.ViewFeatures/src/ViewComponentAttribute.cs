// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates the class and all subclasses are view components. Optionally specifies a view component's name. If
/// defining a base class for multiple view components, associate this attribute with that base.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ViewComponentAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the view component. Do not supply a name in an attribute associated with a view
    /// component base class.
    /// </summary>
    public string Name { get; set; }
}
