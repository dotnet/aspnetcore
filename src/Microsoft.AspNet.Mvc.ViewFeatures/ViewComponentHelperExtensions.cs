// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public static class ViewComponentHelperExtensions
    {
        public static HtmlString Invoke<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            return helper.Invoke(typeof(TComponent), args);
        }

        public static void RenderInvoke<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            helper.RenderInvoke(typeof(TComponent), args);
        }

        public static Task<HtmlString> InvokeAsync<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            return helper.InvokeAsync(typeof(TComponent), args);
        }

        public static Task RenderInvokeAsync<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            return helper.RenderInvokeAsync(typeof(TComponent), args);
        }
    }
}
