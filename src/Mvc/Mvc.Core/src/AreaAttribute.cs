// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies the area containing a controller or action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AreaAttribute : RouteValueAttribute
{
    /// <summary>
    /// Initializes a new <see cref="AreaAttribute"/> instance.
    /// </summary>
    /// <param name="areaName">The area containing the controller or action.</param>
    public AreaAttribute(string areaName)
        : base("area", areaName)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaName);
    }
}
