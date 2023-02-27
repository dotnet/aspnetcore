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
    /// Adds an instance of the specified component and instructs it to render.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>A <see cref="HtmlContent"/> instance representing the render output.</returns>
    public Task<HtmlContent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>() where TComponent : IComponent
        => RenderComponentAsync<TComponent>(ParameterView.Empty);

    /// <summary>
    /// Adds an instance of the specified component and instructs it to render.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>A <see cref="HtmlContent"/> instance representing the render output.</returns>
    public async Task<HtmlContent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        ParameterView parameters) where TComponent : IComponent
    {
        return await _passiveHtmlRenderer.Dispatcher.InvokeAsync(() =>
            _passiveHtmlRenderer.RenderComponentAsync(typeof(TComponent), parameters));
    }
}
