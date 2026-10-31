// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Represents the properties and methods that are needed in order to render a view that uses Razor syntax.
/// </summary>
/// <typeparam name="TModel">The type of the view data model.</typeparam>
public abstract class RazorPage<TModel> : RazorPage
{
    /// <summary>
    /// Gets the Model property of the <see cref="ViewData"/> property.
    /// </summary>
    public TModel Model => ViewData.Model;

    /// <summary>
    /// Gets or sets the dictionary for view data.
    /// </summary>
    [RazorInject]
    public ViewDataDictionary<TModel> ViewData { get; set; } = default!;
}
