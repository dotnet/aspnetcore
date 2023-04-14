// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Microsoft.AspNetCore.Components.Binding;

internal class CascadingModelBinder : IComponent
{
    private RenderHandle _handle;
    private bool _hasRendered;
    private ModelBindingContext? _bindingContext;

    [Parameter] public string Name { get; set; } = default!;

    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (!_hasRendered)
        {
            _hasRendered = true;
            _bindingContext = new ModelBindingContext(Name);
            parameters.SetParameterProperties(this);
            _handle.Render(builder =>
            {
                builder.OpenComponent<CascadingValue<ModelBindingContext>>(0);
                builder.AddComponentParameter(1, "IsFixed", true);
                builder.AddComponentParameter(2, "Value", _bindingContext);
                builder.AddComponentParameter(3, "ChildContent", ChildContent);
                builder.CloseComponent();
            });
        }

        return Task.CompletedTask;
    }
}
