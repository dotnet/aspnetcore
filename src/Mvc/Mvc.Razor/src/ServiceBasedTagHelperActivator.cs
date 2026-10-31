// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// A <see cref="ITagHelperActivator"/> that retrieves tag helpers as services from the request's
/// <see cref="IServiceProvider"/>.
/// </summary>
internal sealed class ServiceBasedTagHelperActivator : ITagHelperActivator
{
    /// <inheritdoc />
    public TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.HttpContext.RequestServices.GetRequiredService<TTagHelper>();
    }
}
