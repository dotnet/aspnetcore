// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// The definition of a Razor Components Application.
/// </summary>
/// <remarks>
/// Typically the top level component (like the App component or the MainLayout component)
/// for the application implements this interface.
/// </remarks> 
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public abstract class RazorComponentApplicationAttribute : Attribute, IRazorComponentApplication
{
    /// <summary>
    /// Creates a builder that can be used to customize the definition of the application.
    /// For example, to add or remove pages, change routes, etc.
    /// </summary>
    /// <returns>
    /// The <see cref="ComponentApplicationBuilder"/> associated with the application
    /// definition.
    /// </returns>
    public abstract ComponentApplicationBuilder GetBuilder();
}
