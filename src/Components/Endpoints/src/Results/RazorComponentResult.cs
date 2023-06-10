// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// An <see cref="IResult"/> that renders a Razor Component.
/// </summary>
public class RazorComponentResult : IResult
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters
        = new Dictionary<string, object?>().AsReadOnly();

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    public RazorComponentResult(Type componentType)
        : this(componentType, null)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(Type componentType, object? parameters)
        : this(componentType, CoerceParametersObjectToDictionary(parameters))
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(Type componentType, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Note that the Blazor renderer will validate that componentType implements IComponent and throws a suitable
        // exception if not, so we don't need to duplicate that logic here.

        ArgumentNullException.ThrowIfNull(componentType);
        ComponentType = componentType;
        Parameters = parameters ?? EmptyParameters;
    }

    private static IReadOnlyDictionary<string, object?>? CoerceParametersObjectToDictionary(object? parameters)
        => parameters is null
        ? null
        : (IReadOnlyDictionary<string, object?>)PropertyHelper.ObjectToDictionary(parameters);

    /// <summary>
    /// Gets the component type.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets the parameters for the component.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets or sets a flag to indicate whether streaming rendering should be prevented. If true, the renderer will
    /// wait for the component hierarchy to complete asynchronous tasks such as loading before supplying the HTML response.
    /// If false, streaming rendering will be determined by the components being rendered.
    ///
    /// The default value is false.
    /// </summary>
    public bool PreventStreamingRendering { get; set; }

    /// <summary>
    /// Requests the service of
    /// <see cref="RazorComponentResultExecutor.ExecuteAsync(HttpContext, RazorComponentResult)" />
    /// to process itself in the given <paramref name="httpContext" />.
    /// </summary>
    /// <param name="httpContext">An <see cref="HttpContext" /> associated with the current request.</param >
    /// <returns >A <see cref="T:System.Threading.Tasks.Task" /> which will complete when execution is completed.</returns >
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var executor = httpContext.RequestServices.GetRequiredService<RazorComponentResultExecutor>();
        return executor.ExecuteAsync(httpContext, this);
    }
}
