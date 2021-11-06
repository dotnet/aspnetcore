// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A context object for <see cref="IPageApplicationModelProvider"/>.
/// </summary>
public class PageApplicationModelProviderContext
{
    /// <summary>
    /// Instantiates a new instance of <see cref="PageApplicationModelProviderContext"/>.
    /// </summary>
    /// <param name="descriptor">The <see cref="PageActionDescriptor"/>.</param>
    /// <param name="pageTypeInfo">The type of the page.</param>
    public PageApplicationModelProviderContext(PageActionDescriptor descriptor, TypeInfo pageTypeInfo)
    {
        ActionDescriptor = descriptor;
        PageType = pageTypeInfo;
    }

    /// <summary>
    /// Gets the <see cref="PageActionDescriptor"/>.
    /// </summary>
    public PageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// Gets the page <see cref="TypeInfo"/>.
    /// </summary>
    public TypeInfo PageType { get; }

    /// <summary>
    /// Gets or sets the <see cref="ApplicationModels.PageApplicationModel"/>.
    /// </summary>
    public PageApplicationModel PageApplicationModel { get; set; } = default!;
}
