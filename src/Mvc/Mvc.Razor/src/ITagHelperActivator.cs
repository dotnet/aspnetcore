// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Provides methods to create a tag helper.
/// </summary>
public interface ITagHelperActivator
{
    /// <summary>
    /// Creates an <see cref="ITagHelper"/>.
    /// </summary>
    /// <typeparam name="TTagHelper">The <see cref="ITagHelper"/> type.</typeparam>
    /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
    /// <returns>The tag helper.</returns>
    TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;
}
