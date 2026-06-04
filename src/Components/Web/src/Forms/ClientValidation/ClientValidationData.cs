// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Renders the per-form &lt;blazor-client-validation-data&gt; carrier element when client-side
/// validation is activated for the surrounding form. Activation is signalled by a validator
/// component (e.g. <see cref="DataAnnotationsValidator"/>) writing a non-null value into
/// <see cref="EditContext.Properties"/> under the key <c>typeof(ClientValidationMarker)</c>.
/// </summary>
/// <remarks>
/// <para>
/// On Blazor SSR, <see cref="EditForm"/> includes one instance of this component at the end of
/// its render tree. On Server/WebAssembly/interactive paths no <see cref="ClientValidationProvider"/>
/// is registered, the optional service lookup returns <see langword="null"/>, and the component
/// is a no-op.
/// </para>
/// <para>
/// The component renders at most once per form instance; <see cref="EditForm"/> never re-parents it,
/// so a single-pass guard is sufficient.
/// </para>
/// </remarks>
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

        // No surrounding EditForm, or no validator requested client validation for this form.
        if (CurrentEditContext is null
            || !CurrentEditContext.Properties.TryGetValue(typeof(ClientValidationMarker), out _))
        {
            return Task.CompletedTask;
        }

        // Optional service: no ClientValidationProvider is registered outside Components.Endpoints,
        // so on Server / WASM / interactive paths the resolved provider is null and we render nothing.
        // Third parties that subclass ClientValidationProvider and register their own concrete are
        // picked up by the same lookup.
        var provider = Services.GetService<ClientValidationProvider>();
        var descriptor = provider?.GetFormDescriptor(CurrentEditContext);
        if (descriptor is null || descriptor.Fields.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Framework-owned internal serializer; owns the JSON wire format. Third-party
        // ClientValidationProvider subclasses never see it - they return the typed descriptor.
        var json = ClientValidationDataSerializer.Serialize(descriptor);

        _handle.Render(builder =>
        {
            builder.OpenElement(0, "blazor-client-validation-data");
            builder.AddMarkupContent(1, json);
            builder.CloseElement();
        });
        return Task.CompletedTask;
    }
}
