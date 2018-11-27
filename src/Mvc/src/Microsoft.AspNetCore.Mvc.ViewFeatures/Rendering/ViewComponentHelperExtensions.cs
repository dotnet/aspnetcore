// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Extension methods for <see cref="IViewComponentHelper"/>.
    /// </summary>
    public static class ViewComponentHelperExtensions
    {
        /// <summary>
        /// Invokes a view component with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IViewComponentHelper"/>.</param>
        /// <param name="name">The name of the view component.</param>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
        /// </returns>
        public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper helper, string name)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(name, arguments: null);
        }

        /// <summary>
        /// Invokes a view component of type <paramref name="componentType" />.
        /// </summary>
        /// <param name="helper">The <see cref="IViewComponentHelper"/>.</param>
        /// <param name="componentType">The view component <see cref="Type"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
        /// </returns>
        public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper helper, Type componentType)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(componentType, arguments: null);
        }

        /// <summary>
        /// Invokes a view component of type <typeparamref name="TComponent"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IViewComponentHelper"/>.</param>
        /// <param name="arguments">Arguments to be passed to the invoked view component method.</param>
        /// <typeparam name="TComponent">The <see cref="Type"/> of the view component.</typeparam>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
        /// </returns>
        public static Task<IHtmlContent> InvokeAsync<TComponent>(this IViewComponentHelper helper, object arguments)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(typeof(TComponent), arguments);
        }

        /// <summary>
        /// Invokes a view component of type <typeparamref name="TComponent"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IViewComponentHelper"/>.</param>
        /// <typeparam name="TComponent">The <see cref="Type"/> of the view component.</typeparam>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
        /// </returns>
        public static Task<IHtmlContent> InvokeAsync<TComponent>(this IViewComponentHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(typeof(TComponent), arguments: null);
        }
    }
}
