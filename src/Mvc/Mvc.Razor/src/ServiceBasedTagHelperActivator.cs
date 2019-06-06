// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// A <see cref="ITagHelperActivator"/> that retrieves tag helpers as services from the request's
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    internal class ServiceBasedTagHelperActivator : ITagHelperActivator
    {
        /// <inheritdoc />
        public TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.HttpContext.RequestServices.GetRequiredService<TTagHelper>();
        }
    }
}
