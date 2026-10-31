// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="ActionResult"/> that renders a view to the response.
/// </summary>
public class ViewResult : ActionResult, IStatusCodeActionResult
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the name or path of the view that is rendered to the response.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, defaults to <see cref="ControllerActionDescriptor.ActionName"/>.
    /// </remarks>
    public string? ViewName { get; set; }

    /// <summary>
    /// Gets the view data model.
    /// </summary>
    public object? Model => ViewData?.Model;

    /// <summary>
    /// Gets or sets the <see cref="ViewDataDictionary"/> for this result.
    /// </summary>
    public ViewDataDictionary ViewData { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ITempDataDictionary"/> for this result.
    /// </summary>
    public ITempDataDictionary TempData { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="IViewEngine"/> used to locate views.
    /// </summary>
    /// <remarks>When <c>null</c>, an instance of <see cref="ICompositeViewEngine"/> from
    /// <c>ActionContext.HttpContext.RequestServices</c> is used.</remarks>
    public IViewEngine? ViewEngine { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; set; }

    /// <inheritdoc />
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetService<IActionResultExecutor<ViewResult>>();
        if (executor == null)
        {
            throw new InvalidOperationException(Mvc.Core.Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                "AddControllersWithViews()",
                "ConfigureServices(...)"));
        }

        await executor.ExecuteAsync(context, this);
    }
}
