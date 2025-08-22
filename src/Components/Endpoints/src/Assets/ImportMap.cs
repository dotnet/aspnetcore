// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents an <c><script type="importmap"></script></c> element that defines the import map for module scripts
/// in the application.
/// </summary>
public sealed class ImportMap : IComponent
{
    private RenderHandle _renderHandle;
    private bool _firstRender = true;
    private ImportMapDefinition? _computedImportMapDefinition;

    /// <summary>
    /// Gets or sets the <see cref="HttpContext"/> for the component.
    /// </summary>
    [CascadingParameter] public HttpContext? HttpContext { get; set; } = null;

    /// <summary>
    /// Gets or sets the import map definition to use for the component. If not set
    /// the component will generate the import map based on the assets defined for this
    /// application.
    /// </summary>
    [Parameter]
    public ImportMapDefinition? ImportMapDefinition { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>script</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (!_firstRender && ReferenceEquals(ImportMapDefinition, _computedImportMapDefinition))
        {
            return Task.CompletedTask;
        }
        else
        {
            _firstRender = false;
            _computedImportMapDefinition = ImportMapDefinition ?? HttpContext?.GetEndpoint()?.Metadata.GetMetadata<ImportMapDefinition>();
            if (_computedImportMapDefinition != null)
            {
                _renderHandle.Render(RenderImportMap);
            }
            return Task.CompletedTask;
        }
    }

    private void RenderImportMap(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "script");
        builder.AddAttribute(1, "type", "importmap");
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddMarkupContent(3, _computedImportMapDefinition!.ToJson());
        builder.CloseElement();
    }
}
