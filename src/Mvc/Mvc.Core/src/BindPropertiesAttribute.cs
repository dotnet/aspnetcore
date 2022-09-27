// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An attribute that enables binding for all properties the decorated controller or Razor Page model defines.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class BindPropertiesAttribute : Attribute
{
    /// <summary>
    /// When <c>true</c>, allows properties to be bound on GET requests. When <c>false</c>, properties
    /// do not get model bound or validated on GET requests.
    /// <para>
    /// Defaults to <c>false</c>.
    /// </para>
    /// </summary>
    public bool SupportsGet { get; set; }
}
