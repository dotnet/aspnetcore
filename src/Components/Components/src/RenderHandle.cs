// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Allows a component to interact with its renderer.
/// </summary>
public readonly struct RenderHandle
{
    private readonly Renderer? _renderer;
    private readonly int _componentId;

    internal RenderHandle(Renderer renderer, int componentId)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _componentId = componentId;
    }

    /// <summary>
    /// Gets the <see cref="Components.Dispatcher" /> associated with the component.
    /// </summary>
    public Dispatcher Dispatcher
    {
        get
        {
            if (_renderer == null)
            {
                ThrowNotInitialized();
            }

            return _renderer.Dispatcher;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="RenderHandle"/> has been
    /// initialized and is ready to use.
    /// </summary>
    public bool IsInitialized => _renderer is not null;

    /// <summary>
    /// Gets a value that determines if the <see cref="Renderer"/> is triggering a render in response to a metadata update (hot-reload) change.
    /// </summary>
    public bool IsRenderingOnMetadataUpdate => HotReloadManager.Default.MetadataUpdateSupported && (_renderer?.IsRenderingOnMetadataUpdate ?? false);

    internal bool IsRendererDisposed => _renderer?.Disposed
        ?? throw new InvalidOperationException("No renderer has been initialized.");

    /// <summary>
    /// Gets the <see cref="Components.RendererInfo"/> the component is running on.
    /// </summary>
    public RendererInfo RendererInfo => _renderer?.RendererInfo ?? throw new InvalidOperationException("No renderer has been initialized.");

    /// <summary>
    /// Retrieves the <see cref="IComponentRenderMode"/> assigned to the component.
    /// </summary>
    /// <returns>The <see cref="IComponentRenderMode"/> assigned to the component.</returns>
    public IComponentRenderMode? RenderMode
    {
        get
        {
            if (_renderer == null)
            {
                throw new InvalidOperationException("No renderer has been initialized.");
            }

            return _renderer.GetComponentRenderMode(_componentId);
        }
    }

    /// <summary>
    /// Gets the <see cref="ResourceAssetCollection"/> associated with the <see cref="Renderer"/>.
    /// </summary>
    public ResourceAssetCollection Assets
    {
        get
        {
            return _renderer?.Assets ?? throw new InvalidOperationException("No renderer has been initialized.");
        }
    }

    /// <summary>
    /// Notifies the renderer that the component should be rendered.
    /// </summary>
    /// <param name="renderFragment">The content that should be rendered.</param>
    public void Render(RenderFragment renderFragment)
    {
        if (_renderer == null)
        {
            ThrowNotInitialized();
        }

        _renderer.AddToRenderQueue(_componentId, renderFragment);
    }

    /// <summary>
    /// Dispatches an <see cref="Exception"/> to the <see cref="Renderer"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that will be dispatched to the renderer.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the exception has finished dispatching.</returns>
    public Task DispatchExceptionAsync(Exception exception)
    {
        var renderer = _renderer;
        var componentId = _componentId;
        return Dispatcher.InvokeAsync(() => renderer!.HandleComponentException(exception, componentId));
    }

    [DoesNotReturn]
    private static void ThrowNotInitialized()
    {
        throw new InvalidOperationException("The render handle is not yet assigned.");
    }
}
