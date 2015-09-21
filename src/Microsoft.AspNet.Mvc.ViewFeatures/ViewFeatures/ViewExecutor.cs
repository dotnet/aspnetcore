// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// Utility type for rendering a <see cref="IView"/> to the response.
    /// </summary>
    public static class ViewExecutor
    {
        public static readonly MediaTypeHeaderValue DefaultContentType = new MediaTypeHeaderValue("text/html")
        {
            Encoding = Encoding.UTF8
        }.CopyAsReadOnly();

        /// <summary>
        /// Asynchronously renders the specified <paramref name="view"/> to the response body.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> to render.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing action.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> for the view being rendered.</param>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> for the view being rendered.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous rendering.</returns>
        public static async Task ExecuteAsync(
            [NotNull] IView view,
            [NotNull] ActionContext actionContext,
            [NotNull] ViewDataDictionary viewData,
            [NotNull] ITempDataDictionary tempData,
            [NotNull] HtmlHelperOptions htmlHelperOptions,
            MediaTypeHeaderValue contentType)
        {
            var response = actionContext.HttpContext.Response;

            if (contentType != null && contentType.Encoding == null)
            {
                // Do not modify the user supplied content type, so copy it instead
                contentType = contentType.Copy();
                contentType.Encoding = Encoding.UTF8;
            }

            // Priority list for setting content-type:
            //      1. passed in contentType (likely set by the user on the result)
            //      2. response.ContentType (likely set by the user in controller code)
            //      3. ViewExecutor.DefaultContentType (sensible default)
            response.ContentType = contentType?.ToString() ?? response.ContentType ?? DefaultContentType.ToString();

            using (var writer = new HttpResponseStreamWriter(response.Body, contentType?.Encoding ?? DefaultContentType.Encoding))
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    tempData,
                    writer,
                    htmlHelperOptions);

                await view.RenderAsync(viewContext);
            }
        }
    }
}