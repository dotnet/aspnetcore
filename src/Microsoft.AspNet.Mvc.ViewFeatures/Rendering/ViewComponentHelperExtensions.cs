// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class ViewComponentHelperExtensions
    {
        public static IHtmlContent Invoke<TComponent>(this IViewComponentHelper helper, params object[] args)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Invoke(typeof(TComponent), args);
        }

        public static void RenderInvoke<TComponent>(this IViewComponentHelper helper, params object[] args)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            helper.RenderInvoke(typeof(TComponent), args);
        }

        public static Task<IHtmlContent> InvokeAsync<TComponent>(
            this IViewComponentHelper helper,
            params object[] args)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(typeof(TComponent), args);
        }

        public static Task RenderInvokeAsync<TComponent>(this IViewComponentHelper helper, params object[] args)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RenderInvokeAsync(typeof(TComponent), args);
        }
    }
}
