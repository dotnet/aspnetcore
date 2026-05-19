// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Renders content provided by <see cref="SectionContent"/> components with matching <see cref="SectionId"/>s.
/// </summary>
public sealed class SectionOutlet : IComponent, IDisposable
{
    private static readonly RenderFragment _emptyRenderFragment = _ => { };
    private object? _subscribedIdentifier;
    private RenderHandle _renderHandle;
    private SectionRegistry _registry = default!;
    private SectionContent? _currentContentProvider;

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

    internal IComponent? CurrentLogicalParent => _currentContentProvider;

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

    internal void ContentUpdated(SectionContent? provider)
    {
        _currentContentProvider = provider;
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

        _renderHandle.Render(BuildRenderTree);
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        var fragment = _currentContentProvider?.ChildContent ?? _emptyRenderFragment;

        builder.OpenComponent<SectionOutletContentRenderer>(0);
        builder.SetKey(fragment);
        builder.AddComponentParameter(1, SectionOutletContentRenderer.ContentParameterName, fragment);
        builder.CloseComponent();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_subscribedIdentifier is not null)
        {
            _registry.Unsubscribe(_subscribedIdentifier);
        }
    }

    // This component simply renders the RenderFragment it is given
    // The reason for rendering SectionOutlet output via this component is so that
    // [1] We can use @key to guarantee that we only preserve descendant component
    //     instances when they come from the same SectionContent, not unrelated ones
    // [2] We know that whenever the SectionContent is changed to another one, there
    //     will be a new ComponentState established to represent this intermediate
    //     component, and it will already have the correct LogicalParentComponentState
    //     so anything computed from this (e.g., whether or not streaming rendering is
    //     enabled) will be freshly re-evaluated, without that information having to
    //     change in place on an existing ComponentState.
    internal sealed class SectionOutletContentRenderer : IComponent
    {
        public const string ContentParameterName = "content";

        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            var fragment = parameters.GetValueOrDefault<RenderFragment>(ContentParameterName)!;
            _renderHandle.Render(fragment);
            return Task.CompletedTask;
        }
    }
}
