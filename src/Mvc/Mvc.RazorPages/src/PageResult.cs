// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// An <see cref="ActionResult"/> that renders a Razor Page.
/// </summary>
public class PageResult : ActionResult
{
    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets the page model.
    /// </summary>
    public object Model => ViewData?.Model!;

    /// <summary>
    /// Gets or sets the <see cref="PageBase"/> to be executed.
    /// </summary>
    public PageBase Page { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ViewDataDictionary"/> for the page to be executed.
    /// </summary>
    public ViewDataDictionary ViewData { get; set; } = default!;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Requests the service of
    /// <see cref="M:Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageResultExecutor.ExecuteAsync(PageContext,PageResult)" />
    /// to process itself in the given <paramref name="context" />.
    /// </summary>
    /// <param name="context">A <see cref="T:Microsoft.AspNetCore.Mvc.RazorPages.PageContext" />
    /// associated with the current request for a Razor page.</param >
    /// <returns >A <see cref="T:System.Threading.Tasks.Task" /> which will complete when page execution is completed.</returns >
    /// <exception cref="T:System.ArgumentException">The parameter <paramref name="context" /> was not a
    /// <see cref="T:Microsoft.AspNetCore.Mvc.RazorPages.PageContext" /></exception >
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
