// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Executes a Razor Page.
    /// </summary>
    internal class PageResultTemplateViewExecutor : ViewTemplateExecutor, IActionResultExecutor<PageResult>
    {
        private readonly IRazorPageActivator _razorPageActivator;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly RazorViewLookup _viewLookup;
        private readonly HtmlEncoder _htmlEncoder;

        public PageResultTemplateViewExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            IViewTemplateFactory templateFactory,
            RazorViewLookup viewLookup,
            IRazorPageActivator razorPageActivator,
            DiagnosticListener diagnosticListener,
            HtmlEncoder htmlEncoder)
            : base(writerFactory, templateFactory, diagnosticListener)
        {
            _viewLookup = viewLookup;
            _htmlEncoder = htmlEncoder;
            _razorPageActivator = razorPageActivator;
            _diagnosticListener = diagnosticListener;
        }

        /// <summary>
        /// Executes a Razor Page asynchronously.
        /// </summary>
        public virtual Task ExecuteAsync(ActionContext actionContext, PageResult result)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(actionContext is PageContext pageContext))
            {
                throw new ArgumentException(Resources.FormatPageViewResult_ContextIsInvalid(
                    nameof(actionContext),
                    nameof(Page),
                    nameof(PageResult)));
            }

            if (result.Model != null)
            {
                pageContext.ViewData.Model = result.Model;
            }

            OnExecuting(pageContext);

            var viewStarts = new IRazorPage[pageContext.ViewStartFactories.Count];
            for (var i = 0; i < pageContext.ViewStartFactories.Count; i++)
            {
                viewStarts[i] = pageContext.ViewStartFactories[i]();
            }

            var viewContext = result.Page.ViewContext;
            var pageAdapter = new RazorPageAdapter(result.Page, pageContext.ActionDescriptor.DeclaredModelTypeInfo);

            var viewTemplatingSystem = new RazorViewTemplatingSystem(
                _viewLookup,
                _razorPageActivator,
                _htmlEncoder,
                _diagnosticListener,
                pageAdapter,
                viewStarts);

            viewTemplatingSystem.OnAfterPageActivated = (page, currentViewContext) =>
            {
                if (page != pageAdapter)
                {
                    return;
                }

                // ViewContext is always activated with the "right" ViewData<T> type.
                // Copy that over to the PageContext since PageContext.ViewData is exposed
                // as the ViewData property on the Page that the user works with.
                pageContext.ViewData = currentViewContext.ViewData;
            };


            return ExecuteAsync(viewContext, viewTemplatingSystem, result.ContentType, result.StatusCode);
        }

        private void OnExecuting(PageContext pageContext)
        {
            var viewDataValuesProvider = pageContext.HttpContext.Features.Get<IViewDataValuesProviderFeature>();
            if (viewDataValuesProvider != null)
            {
                viewDataValuesProvider.ProvideViewDataValues(pageContext.ViewData);
            }
        }
    }
}
