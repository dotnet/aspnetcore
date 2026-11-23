using Microsoft.AspNetCore.Components;

namespace BlazorWebCSharp._1.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusMessageKey = "Identity.StatusMessage";

    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        // Resolve the URI the same way NavigationManager does and only allow destinations within the app's base URI.
        // The base URI always ends with "/", so appending "/" also allows the app root without it (for example "/app?x=1").
        var baseUri = new Uri(navigationManager.BaseUri);
        if (!Uri.TryCreate(baseUri, uri, out var absoluteUri)
            || !(absoluteUri.GetLeftPart(UriPartial.Path) + "/").StartsWith(baseUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
        {
            absoluteUri = baseUri;
        }

        navigationManager.NavigateTo(absoluteUri.AbsoluteUri);
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);
}
