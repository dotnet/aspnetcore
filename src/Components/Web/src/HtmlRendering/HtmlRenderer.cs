// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides a mechanism for rendering components non-interactively as HTML markup.
/// </summary>
public class HtmlRenderer : IAsyncDisposable
{
    private readonly HtmlRendererCore _passiveHtmlRenderer;

    /// <summary>
    /// Constructs an instance of <see cref="HtmlRenderer"/>.
    /// </summary>
    /// <param name="services">The services to use when rendering components.</param>
    /// <param name="loggerFactory">The logger factory to use.</param>
    public HtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        var componentActivator = services.GetService<IComponentActivator>() ?? DefaultComponentActivator.Instance;
        _passiveHtmlRenderer = new HtmlRendererCore(services, loggerFactory, componentActivator);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _passiveHtmlRenderer.DisposeAsync();

    /// <summary>
    /// Gets the <see cref="Components.Dispatcher" /> associated with this instance. Any calls to
    /// <see cref="RenderComponentAsync{TComponent}()"/> or <see cref="BeginRenderingComponent{TComponent}()"/>
    /// must be performed using this <see cref="Components.Dispatcher" />.
    /// </summary>
    public Dispatcher Dispatcher => _passiveHtmlRenderer.Dispatcher;

    /// <summary>
    /// Adds an instance of the specified component and instructs it to render. The resulting content represents the
    /// initial synchronous rendering state, which may later change. To wait for the component hierarchy to complete
    /// any asynchronous operations such as loading, use <see cref="HtmlContent.WaitForQuiescenceAsync"/> before
    /// reading content from the <see cref="HtmlContent"/>.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>A task that completes with <see cref="HtmlContent"/> instance representing the render output.</returns>
    public HtmlContent BeginRenderingComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>() where TComponent : IComponent
        => BeginRenderingComponent<TComponent>(ParameterView.Empty);

    /// <summary>
    /// Adds an instance of the specified component and instructs it to render. The resulting content represents the
    /// initial synchronous rendering state, which may later change. To wait for the component hierarchy to complete
    /// any asynchronous operations such as loading, use <see cref="HtmlContent.WaitForQuiescenceAsync"/> before
    /// reading content from the <see cref="HtmlContent"/>.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="parameters">Parameters for the component.</param>
    /// <returns>A task that completes with <see cref="HtmlContent"/> instance representing the render output.</returns>
    public HtmlContent BeginRenderingComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        ParameterView parameters) where TComponent : IComponent
        => _passiveHtmlRenderer.BeginRenderingComponentAsync(typeof(TComponent), parameters);

    /// <summary>
    /// Adds an instance of the specified component and instructs it to render, waiting
    /// for the component hierarchy to complete asynchronous tasks such as loading.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>A task that completes with <see cref="HtmlContent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
    public Task<HtmlContent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>() where TComponent : IComponent
        => RenderComponentAsync<TComponent>(ParameterView.Empty);

    /// <summary>
    /// Adds an instance of the specified component and instructs it to render, waiting
    /// for the component hierarchy to complete asynchronous tasks such as loading.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="parameters">Parameters for the component.</param>
    /// <returns>A task that completes with <see cref="HtmlContent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
    public async Task<HtmlContent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        ParameterView parameters) where TComponent : IComponent
    {
        var content = BeginRenderingComponent<TComponent>(parameters);
        await content.WaitForQuiescenceAsync();
        return content;
    }
}
