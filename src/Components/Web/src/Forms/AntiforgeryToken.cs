// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Component that renders an antiforgery token as a hidden field.
/// </summary>
public class AntiforgeryToken : IComponent
{
    private RenderHandle _handle;
    private bool _hasRendered;
    private AntiforgeryRequestToken? _requestToken;

    [Inject] IServiceProvider Services { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
        _requestToken = Services.GetService<AntiforgeryStateProvider>()?.GetAntiforgeryToken();
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (!_hasRendered)
        {
            _hasRendered = true;
            if (_requestToken != null)
            {
                _handle.Render(RenderField);
            }
        }

        return Task.CompletedTask;
    }

    private void RenderField(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", "hidden");
        builder.AddAttribute(2, "name", _requestToken!.FormFieldName);
        builder.AddAttribute(3, "value", _requestToken.Value);
        builder.CloseElement();
    }
}
