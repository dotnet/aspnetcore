// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor.Internal;

/// <summary>
/// Specifies that the attributed property should be bound using request services.
/// <para>
/// This attribute is used as the backing attribute for the <c>@inject</c>
/// Razor directive.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RazorInjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the object that specifies the key of the service to inject.
    /// </summary>
    public object? Key { get; init; }
}
