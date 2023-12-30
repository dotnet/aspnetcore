// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// A filter that handles OPTIONS requests page when no handler method is available.
/// <para>
/// a) MVC treats no handler being selected no differently than a page having no handler, both execute the
/// page.
/// b) A common model for programming Razor Pages is to initialize content required by a page in the
/// <c>OnGet</c> handler. Executing a page without running the handler may result in runtime exceptions -
/// e.g. null ref or out of bounds exception if you expected a property or collection to be initialized.
/// </para>
/// <para>
/// Some web crawlers use OPTIONS request when probing servers. In the absence of an uncommon <c>OnOptions</c>
/// handler, executing the page will likely result in runtime errors as described in earlier. This filter
/// attempts to avoid this pit of failure by handling OPTIONS requests and returning a 200 if no handler is selected.
/// </para>
/// </summary>
internal sealed class HandleOptionsRequestsPageFilter : IPageFilter, IOrderedFilter
{
    /// <summary>
    /// Ordered to run after filters with default order.
    /// </summary>
    public int Order => 1000;

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.HandlerMethod == null &&
            context.Result == null &&
            HttpMethods.IsOptions(context.HttpContext.Request.Method))
        {
            context.Result = new OkResult();
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
