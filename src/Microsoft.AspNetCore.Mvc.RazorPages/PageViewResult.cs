// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// An <see cref="ActionResult"/> that renders a Razor Page.
    /// </summary>
    public class PageViewResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageViewResult"/>.
        /// </summary>
        /// <param name="page">The <see cref="RazorPages.Page"/> to render.</param>
        public PageViewResult(Page page)
        {
            Page = page;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PageViewResult"/> with the specified <paramref name="model"/>.
        /// </summary>
        /// <param name="page">The <see cref="RazorPages.Page"/> to render.</param>
        /// <param name="model">The page model.</param>
        public PageViewResult(Page page, object model)
        {
            Page = page;
            Model = model;
        }

        /// <summary>
        /// Gets or sets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the page model.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// Gets the <see cref="RazorPages.Page"/> to execute.
        /// </summary>
        public Page Page { get; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (!object.ReferenceEquals(context, Page.PageContext))
            {
                throw new ArgumentException(
                    Resources.FormatPageViewResult_ContextIsInvalid(nameof(context), nameof(Page)));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<PageResultExecutor>();
            return executor.ExecuteAsync(Page.PageContext, this);
        }
    }
}