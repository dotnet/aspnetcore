// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Provides content to <see cref="SectionOutlet"/> components with matching <see cref="Name"/>s.
/// </summary>
internal sealed class SectionContent : ISectionContentProvider, IComponent, IDisposable
{
    private string? _registeredName;
    private SectionRegistry _registry = default!;

    /// <summary>
    /// Gets or sets the name that determines which <see cref="SectionOutlet"/> instance will render
    /// the content of this instance.
    /// </summary>
    [Parameter] public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether this component should provide the default content for the target
    /// <see cref="SectionOutlet"/>.
    /// </summary>
    [Parameter] public bool IsDefaultContent { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered in corresponding <see cref="SectionOutlet"/> instances.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    RenderFragment? ISectionContentProvider.Content => ChildContent;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _registry = renderHandle.Dispatcher.SectionRegistry;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"{GetType()} requires a non-empty string parameter '{nameof(Name)}'.");
        }

        if (Name != _registeredName)
        {
            if (_registeredName is not null)
            {
                _registry.RemoveProvider(_registeredName, this);
            }

            _registry.AddProvider(Name, this, IsDefaultContent);
            _registeredName = Name;
        }

        _registry.NotifyContentChanged(Name, this);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_registeredName is not null)
        {
            _registry.RemoveProvider(_registeredName, this);
        }
    }
}
