// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// An <see cref="ActionResult"/> that renders a Razor Component.
/// </summary>
public class RazorComponentResult : ActionResult
{
    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    public RazorComponentResult(Type componentType)
    {
        // Note that the Blazor renderer will validate that componentType implements IComponent and throws a suitable
        // exception if not, so we don't need to duplicate that logic here.

        ArgumentNullException.ThrowIfNull(componentType);
        ComponentType = componentType;
    }

    /// <summary>
    /// Gets the component type.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the parameters for the component.
    /// </summary>
    public ParameterView Parameters { get; set; } = ParameterView.Empty;

    /// <summary>
    /// Gets or sets the rendering mode.
    /// </summary>
    public RenderMode RenderMode { get; set; } = RenderMode.Static;

    /// <summary>
    /// Requests the service of
    /// <see cref="RazorComponentResultExecutor.ExecuteAsync(ActionContext, RazorComponentResult)" />
    /// to process itself in the given <paramref name="context" />.
    /// </summary>
    /// <param name="context">An <see cref="ActionContext" /> associated with the current request for a Razor Component.</param >
    /// <returns >A <see cref="T:System.Threading.Tasks.Task" /> which will complete when execution is completed.</returns >
    public override Task ExecuteResultAsync(ActionContext context)
    {
        var executor = context.HttpContext.RequestServices.GetRequiredService<RazorComponentResultExecutor>();
        return executor.ExecuteAsync(context, this);
    }
}
