// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// Executes a Razor Page.
/// </summary>
public class PageResultExecutor : ViewExecutor
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IRazorPageActivator _razorPageActivator;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly HtmlEncoder _htmlEncoder;

    /// <summary>
    /// Creates a new <see cref="PageResultExecutor"/>.
    /// </summary>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    /// <param name="compositeViewEngine">The <see cref="ICompositeViewEngine"/>.</param>
    /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
    /// <param name="razorPageActivator">The <see cref="IRazorPageActivator"/>.</param>
    /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    public PageResultExecutor(
        IHttpResponseStreamWriterFactory writerFactory,
        ICompositeViewEngine compositeViewEngine,
        IRazorViewEngine razorViewEngine,
        IRazorPageActivator razorPageActivator,
        DiagnosticListener diagnosticListener,
        HtmlEncoder htmlEncoder)
        : base(writerFactory, compositeViewEngine, diagnosticListener)
    {
        _razorViewEngine = razorViewEngine;
        _htmlEncoder = htmlEncoder;
        _razorPageActivator = razorPageActivator;
        _diagnosticListener = diagnosticListener;
    }

    /// <summary>
    /// Executes a Razor Page asynchronously.
    /// </summary>
    public virtual Task ExecuteAsync(PageContext pageContext, PageResult result)
    {
        ArgumentNullException.ThrowIfNull(pageContext);
        ArgumentNullException.ThrowIfNull(result);

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
        var pageAdapter = new RazorPageAdapter(result.Page, pageContext.ActionDescriptor.DeclaredModelTypeInfo!);

        viewContext.View = new RazorView(
            _razorViewEngine,
            _razorPageActivator,
            viewStarts,
            pageAdapter,
            _htmlEncoder,
            _diagnosticListener)
        {
            OnAfterPageActivated = (page, currentViewContext) =>
            {
                if (page != pageAdapter)
                {
                    return;
                }

                // ViewContext is always activated with the "right" ViewData<T> type.
                // Copy that over to the PageContext since PageContext.ViewData is exposed
                // as the ViewData property on the Page that the user works with.
                pageContext.ViewData = currentViewContext.ViewData;
            },
        };

        return ExecuteAsync(viewContext, result.ContentType, result.StatusCode);
    }

    private static void OnExecuting(PageContext pageContext)
    {
        var viewDataValuesProvider = pageContext.HttpContext.Features.Get<IViewDataValuesProviderFeature>();
        viewDataValuesProvider?.ProvideViewDataValues(pageContext.ViewData);
    }
}
