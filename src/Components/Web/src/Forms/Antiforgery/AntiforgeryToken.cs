// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Antiforgery;

internal class AntiforgeryToken : IComponent, IDisposable
{
    private RenderHandle _handle;
    private bool _hasRendered;
    private AntiforgeryRequestToken? _requestToken;

    [Inject] AntiforgeryStateProvider Antiforgery { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
        Antiforgery.AntiforgeryTokenChanged += UpdateToken;
        UpdateToken(Antiforgery.GetAntiforgeryToken());
    }

    private void UpdateToken(AntiforgeryRequestToken token)
    {
        ArgumentNullException.ThrowIfNull(nameof(token));
        _requestToken = token;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (!_hasRendered)
        {
            _hasRendered = true;
            _handle.Render(RenderField);
        }

        return Task.CompletedTask;
    }

    private void RenderField(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", "hidden");
        builder.AddAttribute(2, "name", _requestToken?.FormFieldName);
        builder.AddAttribute(3, "value", _requestToken?.Value);
        builder.CloseElement();
    }

    public void Dispose()
    {
        Antiforgery.AntiforgeryTokenChanged -= UpdateToken;
    }
}
