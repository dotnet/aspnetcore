using Microsoft.AspNetCore.Components;

namespace BlazorWebCSharp._1.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusMessageKey = "Identity.StatusMessage";

    public void RedirectTo(string? uri)
    {
        // The URI often comes from a "returnUrl" query string parameter, which anyone can set in a link to the app.
        // To prevent open redirects, resolve it to an absolute URI and only use it if it's within the app.
        // Appending "/" to the path also accepts the app's root URI without a trailing slash, such as "/app?page=2".
        var baseUri = new Uri(navigationManager.BaseUri);
        if (!Uri.TryCreate(baseUri, uri, out var redirectUri)
            || !(redirectUri.GetLeftPart(UriPartial.Path) + "/").StartsWith(baseUri.AbsoluteUri, StringComparison.Ordinal))
        {
            redirectUri = baseUri;
        }

        navigationManager.NavigateTo(redirectUri.AbsoluteUri);
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
