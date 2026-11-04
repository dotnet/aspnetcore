// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure;

/// <summary>
/// Default implementation of <see cref="ITagHelperActivator"/>.
/// </summary>
internal sealed class DefaultTagHelperActivator : ITagHelperActivator
{
    /// <inheritdoc />
    public TTagHelper Create<TTagHelper>(ViewContext context)
        where TTagHelper : ITagHelper
    {
        ArgumentNullException.ThrowIfNull(context);

        return Cache<TTagHelper>.Create(context.HttpContext.RequestServices);
    }

    private static class Cache<TTagHelper>
    {
        private static readonly ObjectFactory _objectFactory = ActivatorUtilities.CreateFactory(typeof(TTagHelper), Type.EmptyTypes);

        public static TTagHelper Create(IServiceProvider serviceProvider) => (TTagHelper)_objectFactory(serviceProvider, arguments: null);
    }
}
