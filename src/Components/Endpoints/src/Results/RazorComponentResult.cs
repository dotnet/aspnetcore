// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.Internal;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that renders a Razor Component.
/// </summary>
public class RazorComponentResult : IResult, IStatusCodeHttpResult, IContentTypeHttpResult
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters
        = new Dictionary<string, object?>().AsReadOnly();

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    public RazorComponentResult([DynamicallyAccessedMembers(Component)] Type componentType)
        : this(componentType, ReadOnlyDictionary<string, object?>.Empty)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(
        [DynamicallyAccessedMembers(Component)] Type componentType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] object parameters)
        : this(componentType, CoerceParametersObjectToDictionary(parameters))
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="componentType">The type of the component to render. This must implement <see cref="IComponent"/>.</param>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(
        [DynamicallyAccessedMembers(Component)] Type componentType,
        IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        // Note that the Blazor renderer will validate that componentType implements IComponent and throws a suitable
        // exception if not, so we don't need to duplicate that logic here.
        ComponentType = componentType;
        Parameters = parameters ?? EmptyParameters;
    }

    private static IReadOnlyDictionary<string, object?> CoerceParametersObjectToDictionary(object? parameters)
        => parameters is null
        ? throw new ArgumentNullException(nameof(parameters))
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
    /// Processes this result in the given <paramref name="httpContext" />.
    /// </summary>
    /// <param name="httpContext">An <see cref="HttpContext" /> associated with the current request.</param >
    /// <returns >A <see cref="T:System.Threading.Tasks.Task" /> which will complete when execution is completed.</returns >
    public Task ExecuteAsync(HttpContext httpContext)
        => RazorComponentResultExecutor.ExecuteAsync(httpContext, this);
}
