// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Executes a Razor Page.
    /// </summary>
    public class PageResultExecutor : ViewExecutor
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IRazorPageActivator _razorPageActivator;
        private readonly HtmlEncoder _htmlEncoder;

        /// <summary>
        /// Creates a new <see cref="PageResultExecutor"/>.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="compositeViewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
        /// <param name="razorPageActivator">The <see cref="IRazorPageActivator"/>.</param>
        /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        public PageResultExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine compositeViewEngine,
            IRazorViewEngine razorViewEngine,
            IRazorPageActivator razorPageActivator,
            DiagnosticSource diagnosticSource,
            HtmlEncoder htmlEncoder)
            : base(writerFactory, compositeViewEngine, diagnosticSource)
        {
            _razorViewEngine = razorViewEngine;
            _htmlEncoder = htmlEncoder;
            _razorPageActivator = new PassThruRazorPageActivator(razorPageActivator);
        }

        /// <summary>
        /// Executes a Razor Page asynchronously.
        /// </summary>
        public virtual Task ExecuteAsync(PageContext pageContext, PageResult result)
        {
            if (pageContext == null)
            {
                throw new ArgumentNullException(nameof(pageContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Model != null)
            {
                pageContext.ViewData.Model = result.Model;
            }

            var view = new RazorView(_razorViewEngine, _razorPageActivator, pageContext.ViewStarts, result.Page, _htmlEncoder);
            pageContext.View = view;
            return ExecuteAsync(pageContext, result.ContentType, result.StatusCode);
        }
    }
}
