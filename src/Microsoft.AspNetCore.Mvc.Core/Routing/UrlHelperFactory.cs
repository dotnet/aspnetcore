// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// A default implementation of <see cref="IUrlHelperFactory"/>.
    /// </summary>
    public class UrlHelperFactory : IUrlHelperFactory
    {
        /// <inheritdoc />
        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            var httpContext = context.HttpContext;

            if (httpContext == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ActionContext.HttpContext),
                    nameof(ActionContext)));
            }

            if (httpContext.Items == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(HttpContext.Items),
                    nameof(HttpContext)));
            }

            // Perf: Create only one UrlHelper per context
            object value;
            if (httpContext.Items.TryGetValue(typeof(IUrlHelper), out value) && value is IUrlHelper)
            {
                return (IUrlHelper)value;
            }

            var urlHelper = new UrlHelper(context);
            httpContext.Items[typeof(IUrlHelper)] = urlHelper;

            return urlHelper;
        }
    }
}
