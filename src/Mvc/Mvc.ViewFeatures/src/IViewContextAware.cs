// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Contract for contextualizing a property activated by a view with the <see cref="ViewContext"/>.
/// </summary>
/// <remarks>This interface is used for contextualizing properties added to a Razor page using <c>@inject</c>.</remarks>
public interface IViewContextAware
{
    /// <summary>
    /// Contextualizes the instance with the specified <paramref name="viewContext"/>.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
    void Contextualize(ViewContext viewContext);
}
