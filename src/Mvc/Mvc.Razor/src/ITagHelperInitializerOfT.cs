// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Initializes an <see cref="ITagHelper"/> before it's executed.
/// </summary>
/// <typeparam name="TTagHelper">The <see cref="ITagHelper"/> type.</typeparam>
public interface ITagHelperInitializer<TTagHelper>
    where TTagHelper : ITagHelper
{
    /// <summary>
    /// Initializes the <typeparamref name="TTagHelper"/>.
    /// </summary>
    /// <param name="helper">The <typeparamref name="TTagHelper"/> to initialize.</param>
    /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
    void Initialize(TTagHelper helper, ViewContext context);
}
