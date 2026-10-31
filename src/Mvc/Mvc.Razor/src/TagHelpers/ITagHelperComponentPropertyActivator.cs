// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

/// <summary>
/// Provides methods to activate properties of <see cref="ITagHelperComponent"/>s.
/// </summary>
public interface ITagHelperComponentPropertyActivator
{
    /// <summary>
    /// Activates properties of the <paramref name="tagHelperComponent"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
    /// <param name="tagHelperComponent">The <see cref="ITagHelperComponent"/> to activate properties of.</param>
    void Activate(ViewContext context, ITagHelperComponent tagHelperComponent);
}
