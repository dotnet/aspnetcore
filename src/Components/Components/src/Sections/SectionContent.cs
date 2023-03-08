// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Provides content to <see cref="SectionOutlet"/> components with matching <see cref="SectionId"/>s.
/// </summary>
public sealed class SectionContent : ISectionContentProvider, IComponent, IDisposable
{
    private object? _registeredSectionId;
    private bool? _registeredIsDefaultContent;
    private SectionRegistry _registry = default!;

    /// <summary>
    /// Gets or sets the ID that determines which <see cref="SectionOutlet"/> instance will render
    /// the content of this instance.
    /// </summary>
    [Parameter, EditorRequired] public object SectionId { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether this component should provide the default content for the target
    /// <see cref="SectionOutlet"/>.
    /// </summary>
    internal bool IsDefaultContent { get; set; }

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
        foreach (var param in parameters)
        {
            switch (param.Name)
            {
                case nameof(SectionContent.SectionId):
                    SectionId = param.Value;
                    break;
                case nameof(SectionContent.IsDefaultContent):
                    IsDefaultContent = (bool)param.Value;
                    break;
                case nameof(SectionContent.ChildContent):
                    ChildContent = (RenderFragment)param.Value;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter '{param.Name}'");
            }
        }

        if (SectionId is null)
        {
            throw new InvalidOperationException($"{nameof(SectionContent)} requires a non-null value for the parameter '{nameof(SectionId)}'.");
        }

        if (!object.Equals(SectionId, _registeredSectionId) || IsDefaultContent != _registeredIsDefaultContent)
        {
            if (_registeredSectionId is not null)
            {
                _registry.RemoveProvider(_registeredSectionId, this);
            }

            _registry.AddProvider(SectionId, this, IsDefaultContent);
            _registeredSectionId = SectionId;
            _registeredIsDefaultContent = IsDefaultContent;
        }

        _registry.NotifyContentChanged(SectionId, this);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_registeredSectionId is not null)
        {
            _registry.RemoveProvider(_registeredSectionId, this);
        }
    }
}
