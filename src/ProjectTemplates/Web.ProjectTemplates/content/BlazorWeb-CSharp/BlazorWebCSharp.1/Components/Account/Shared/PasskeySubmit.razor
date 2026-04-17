@using Microsoft.AspNetCore.Antiforgery
@inject IServiceProvider Services

<button type="submit" name="__passkeySubmit" @attributes="AdditionalAttributes">@ChildContent</button>
<passkey-submit
    operation="@Operation"
    name="@Name"
    email-name="@EmailName"
    request-token-name="@tokens?.HeaderName"
    request-token-value="@tokens?.RequestToken">
</passkey-submit>

@code {
    private AntiforgeryTokenSet? tokens;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public PasskeyOperation Operation { get; set; }

    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = default!;

    [Parameter]
    public string? EmailName { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnInitialized()
    {
        tokens = Services.GetService<IAntiforgery>()?.GetTokens(HttpContext);
    }
}
