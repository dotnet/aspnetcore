// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A service that can prerender Razor Components as HTML.
/// </summary>
public interface IComponentPrerenderer
{
    /// <summary>
    /// Prerenders a Razor Component as HTML.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="componentType">The type of component to prerender. This must implement <see cref="IComponent"/>.</param>
    /// <param name="renderMode">The mode in which to prerender the component.</param>
    /// <param name="parameters">Parameters for the component.</param>
    /// <returns>A task that completes with the prerendered content.</returns>
    ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        [DynamicallyAccessedMembers(Component)] Type componentType,
        IComponentRenderMode renderMode,
        ParameterView parameters);

    /// <summary>
    /// Prepares a serialized representation of any component state that is persistible within the current request.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="serializationMode">The <see cref="PersistedStateSerializationMode"/>.</param>
    /// <returns>A task that completes with the prerendered state content.</returns>
    ValueTask<IHtmlContent> PrerenderPersistedStateAsync(
        HttpContext httpContext,
        PersistedStateSerializationMode serializationMode);

    /// <summary>
    /// Gets a <see cref="Dispatcher"/> that should be used for calls to <see cref="PrerenderComponentAsync(HttpContext, Type, IComponentRenderMode, ParameterView)"/>.
    /// </summary>
    Dispatcher Dispatcher { get; }
}
