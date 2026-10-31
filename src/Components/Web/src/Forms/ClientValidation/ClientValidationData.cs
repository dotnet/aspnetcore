// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Renders the per-form client-side validation carrier when client-side validation is activated
/// for the surrounding form and the registered <see cref="ClientValidationProvider"/> produces some content.
/// </summary>
internal sealed class ClientValidationData : IComponent
{
    private RenderHandle _handle;
    private bool _hasRendered;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter] private EditContext? CurrentEditContext { get; set; }

    public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (_hasRendered)
        {
            return Task.CompletedTask;
        }

        _hasRendered = true;

        // No surrounding EditForm, or no validator activated client validation for this form.
        if (CurrentEditContext is null
            || !CurrentEditContext.Properties.TryGetValue(typeof(DataAnnotationsValidator), out _))
        {
            return Task.CompletedTask;
        }

        var provider = Services.GetService<ClientValidationProvider>();
        if (provider is null)
        {
            return Task.CompletedTask;
        }

        // Inputs that rendered under this EditContext registered their field + HTML name into the registry.
        var registry = RenderedFieldRegistry.Get(CurrentEditContext);
        if (registry is null || registry.Fields.Count == 0)
        {
            return Task.CompletedTask;
        }

        var fragment = provider.RenderClientValidationRules(CurrentEditContext, registry.Fields);
        if (fragment is not null)
        {
            _handle.Render(fragment);
        }

        return Task.CompletedTask;
    }
}
