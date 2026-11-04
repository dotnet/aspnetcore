// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Selects a view component based on a view component name.
/// </summary>
public interface IViewComponentSelector
{
    /// <summary>
    /// Selects a view component based on <paramref name="componentName"/>.
    /// </summary>
    /// <param name="componentName">The view component name.</param>
    /// <returns>A <see cref="ViewComponentDescriptor"/>, or <c>null</c> if no match is found.</returns>
    ViewComponentDescriptor SelectComponent(string componentName);
}
