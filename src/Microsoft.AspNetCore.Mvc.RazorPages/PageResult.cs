// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// An <see cref="ActionResult"/> that renders a Razor Page.
    /// </summary>
    public class PageResult : ActionResult
    {
        /// <summary>
        /// Gets or sets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the page model.
        /// </summary>
        public object Model => ViewData?.Model;

        /// <summary>
        /// Gets or sets the <see cref="PageBase"/> to be executed.
        /// </summary>
        public PageBase Page { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/> for the page to be executed.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (!(context is PageContext pageContext))
            {
                throw new ArgumentException(Resources.FormatPageViewResult_ContextIsInvalid(
                    nameof(context),
                    nameof(Page),
                    nameof(PageResult)));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<PageResultExecutor>();
            return executor.ExecuteAsync(pageContext, this);
        }
    }
}