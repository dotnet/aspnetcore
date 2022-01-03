// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Discovers the view components in the application.
/// </summary>
public interface IViewComponentDescriptorProvider
{
    /// <summary>
    /// Gets the set of <see cref="ViewComponentDescriptor"/>.
    /// </summary>
    /// <returns>A list of <see cref="ViewComponentDescriptor"/>.</returns>
    IEnumerable<ViewComponentDescriptor> GetViewComponents();
}
