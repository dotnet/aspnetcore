// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Renders content provided by <see cref="SectionContent"/> components with matching <see cref="Name"/>s.
/// </summary>
internal sealed class SectionOutlet : ISectionContentSubscriber, IComponent, IDisposable
{
    private static readonly RenderFragment _emptyRenderFragment = _ => { };

    private string? _subscribedName;
    private RenderHandle _renderHandle;
    private SectionRegistry _registry = default!;
    private RenderFragment? _content;

    /// <summary>
    /// Gets or sets the name that determines which <see cref="SectionContent"/> instances will provide
    /// content to this instance.
    /// </summary>
    [Parameter] public string Name { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
        _registry = _renderHandle.Dispatcher.SectionRegistry;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"{GetType()} requires a non-empty string parameter '{nameof(Name)}'.");
        }

        if (Name != _subscribedName)
        {
            if (_subscribedName is not null)
            {
                _registry.Unsubscribe(_subscribedName);
            }

            _registry.Subscribe(Name, this);
            _subscribedName = Name;
        }

        RenderContent();

        return Task.CompletedTask;
    }

    void ISectionContentSubscriber.ContentChanged(RenderFragment? content)
    {
        _content = content;
        RenderContent();
    }

    private void RenderContent()
    {
        // Here, we guard against rendering after the renderer has been disposed.
        // This can occur after prerendering or when the page is refreshed.
        // In these cases, a no-op is preferred.
        if (_renderHandle.IsRendererDisposed)
        {
            return;
        }

        _renderHandle.Render(_content ?? _emptyRenderFragment);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_subscribedName is not null)
        {
            _registry.Unsubscribe(_subscribedName);
        }
    }
}
