@if (!string.IsNullOrEmpty(DisplayMessage))
{
    var statusMessageClass = DisplayMessage.StartsWith("Error") ? "danger" : "success";
    <div class="alert alert-@statusMessageClass" role="alert">
        @DisplayMessage
    </div>
}

@code {
    private string? messageFromCookie;

    [Parameter]
    public string? Message { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private string? DisplayMessage => Message ?? messageFromCookie;

    protected override void OnInitialized()
    {
        messageFromCookie = HttpContext.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if (messageFromCookie is not null)
        {
            HttpContext.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }
}
