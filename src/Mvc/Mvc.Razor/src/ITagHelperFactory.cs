// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Provides methods to create and initialize tag helpers.
/// </summary>
public interface ITagHelperFactory
{
    /// <summary>
    /// Creates a new tag helper for the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context"><see cref="ViewContext"/> for the executing view.</param>
    /// <returns>The tag helper.</returns>
    TTagHelper CreateTagHelper<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;
}
