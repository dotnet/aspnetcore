// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

/// <summary>
/// Provides content to <see cref="SectionOutlet"/> components with matching <see cref="SectionId"/>s.
/// </summary>
public sealed class SectionContent : IComponent, IDisposable
{
    private object? _registeredIdentifier;
    private bool? _registeredIsDefaultContent;
    private SectionRegistry _registry = default!;

    /// <summary>
    /// Gets or sets the <see cref="string"/> ID that determines which <see cref="SectionOutlet"/> instance will render
    /// the content of this instance.
    /// </summary>
    [Parameter] public string? SectionName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="object"/> ID that determines which <see cref="SectionOutlet"/> instance will render
    /// the content of this instance.
    /// </summary>
    [Parameter] public object? SectionId { get; set; }

    /// <summary>
    /// Gets or sets whether this component should provide the default content for the target
    /// <see cref="SectionOutlet"/>.
    /// </summary>
    internal bool IsDefaultContent { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered in corresponding <see cref="SectionOutlet"/> instances.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _registry = renderHandle.Dispatcher.SectionRegistry;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        // We are not using parameters.SetParameterProperties(this)
        // because IsDefaultContent is internal property and not a parameter
        SetParameterValues(parameters);

        object? identifier;

        if (SectionName is not null && SectionId is not null)
        {
            throw new InvalidOperationException($"{nameof(SectionContent)} requires that '{nameof(SectionName)}' and '{nameof(SectionId)}' cannot both have non-null values.");
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
            throw new InvalidOperationException($"{nameof(SectionContent)} requires a non-null value either for '{nameof(SectionName)}' or '{nameof(SectionId)}'.");
        }

        if (!object.Equals(identifier, _registeredIdentifier) || IsDefaultContent != _registeredIsDefaultContent)
        {
            if (_registeredIdentifier is not null)
            {
                _registry.RemoveProvider(_registeredIdentifier, this);
            }

            _registry.AddProvider(identifier, this, IsDefaultContent);
            _registeredIdentifier = identifier;
            _registeredIsDefaultContent = IsDefaultContent;
        }

        _registry.NotifyContentProviderChanged(identifier, this);

        return Task.CompletedTask;
    }

    private void SetParameterValues(in ParameterView parameters)
    {
        foreach (var param in parameters)
        {
            switch (param.Name)
            {
                case nameof(SectionContent.SectionName):
                    SectionName = (string)param.Value;
                    break;
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
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_registeredIdentifier is not null)
        {
            _registry.RemoveProvider(_registeredIdentifier, this);
        }
    }
}
