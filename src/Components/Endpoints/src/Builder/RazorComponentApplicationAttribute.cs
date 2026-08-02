// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Discovery;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Indicates how to collect the components that are part of a razor components
/// application.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
internal abstract class RazorComponentApplicationAttribute : Attribute, IRazorComponentApplication
{
    /// <summary>
    /// Creates a builder that can be used to customize the definition of the application.
    /// For example, to add or remove pages, change routes, etc.
    /// </summary>
    /// <returns>
    /// The <see cref="ComponentApplicationBuilder"/> associated with the application definition.
    /// </returns>
    public abstract ComponentApplicationBuilder GetBuilder();
}
