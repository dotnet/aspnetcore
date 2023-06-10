// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Renders content provided by <see cref="SectionContent"/> components with matching <see cref="SectionId"/>s.
/// </summary>
[StreamRendering(true)] // Because the content may be provided by a streaming component
public sealed class SectionOutlet : ISectionContentSubscriber, IComponent, IDisposable
{
    private static readonly RenderFragment _emptyRenderFragment = _ => { };

    private object? _subscribedIdentifier;
    private RenderHandle _renderHandle;
    private SectionRegistry _registry = default!;
    private RenderFragment? _content;

    /// <summary>
    /// Gets or sets the <see cref="string"/> ID that determines which <see cref="SectionContent"/> instances will provide
    /// content to this instance.
    /// </summary>
    [Parameter] public string? SectionName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="object"/> ID that determines which <see cref="SectionContent"/> instances will provide
    /// content to this instance.
    /// </summary>
    [Parameter] public object? SectionId { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
        _registry = _renderHandle.Dispatcher.SectionRegistry;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        object? identifier;

        if (SectionName is not null && SectionId is not null)
        {
            throw new InvalidOperationException($"{nameof(SectionOutlet)} requires that '{nameof(SectionName)}' and '{nameof(SectionId)}' cannot both have non-null values.");
        }
        else if (SectionName is not null)
        {
            identifier = SectionName;
        }
        else if (SectionId is not null)
        {
            identifier = SectionId;
        }
        else
        {
            throw new InvalidOperationException($"{nameof(SectionOutlet)} requires a non-null value either for '{nameof(SectionName)}' or '{nameof(SectionId)}'.");
        }

        if (!object.Equals(identifier, _subscribedIdentifier))
        {
            if (_subscribedIdentifier is not null)
            {
                _registry.Unsubscribe(_subscribedIdentifier);
            }

            _registry.Subscribe(identifier, this);
            _subscribedIdentifier = identifier;
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
        if (_subscribedIdentifier is not null)
        {
            _registry.Unsubscribe(_subscribedIdentifier);
        }
    }
}
