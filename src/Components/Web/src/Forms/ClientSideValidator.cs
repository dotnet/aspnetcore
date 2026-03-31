// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Enables client-side validation for the enclosing <see cref="EditForm"/>.
/// Stores an <see cref="IClientValidationService"/> on the <see cref="EditContext.Properties"/>
/// that causes input components to emit <c>data-val-*</c> HTML attributes, and optionally
/// renders the validation script tag.
/// </summary>
/// <example>
/// <code>
/// &lt;EditForm Model="Contact" FormName="contact" Enhance&gt;
///     &lt;ClientSideValidator /&gt;
///     &lt;InputText @bind-Value="Contact.Name" /&gt;
///     &lt;ValidationMessage For="() =&gt; Contact.Name" /&gt;
///     &lt;button type="submit"&gt;Submit&lt;/button&gt;
/// &lt;/EditForm&gt;
/// </code>
/// </example>
public sealed class ClientSideValidator : ComponentBase, IDisposable
{
    internal static readonly object ServiceKey = typeof(IClientValidationService);

    /// <summary>
    /// Gets or sets whether to automatically render a <c>&lt;script&gt;</c>
    /// tag referencing the validation library. Defaults to <see langword="true"/>.
    /// </summary>
    [Parameter]
    public bool IncludeScript { get; set; } = true;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Inject]
    private IClientValidationService ValidationService { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ClientSideValidator)} requires a cascading parameter of type " +
                $"{nameof(EditContext)}. Use it inside an {nameof(EditForm)}.");
        }

        CurrentEditContext.Properties[ServiceKey] = ValidationService;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (IncludeScript)
        {
            builder.OpenElement(0, "script");
            builder.AddAttribute(1, "src", "_content/Microsoft.AspNetCore.Components.Web/aspnet-core-validation.js");
            builder.CloseElement();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CurrentEditContext?.Properties.Remove(ServiceKey);
    }
}
