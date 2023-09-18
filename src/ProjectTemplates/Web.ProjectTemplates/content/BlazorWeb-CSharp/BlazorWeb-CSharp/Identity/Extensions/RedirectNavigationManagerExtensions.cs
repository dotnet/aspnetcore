using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

internal static class RedirectNavigationManagerExtensions
{
    // These extension methods help make more concise the common case of redirecting to the same or different page
    // with a fresh set of query parameters. An exception is thrown if the current page is not being rendered from
    // an endpoint, because it's not possible to redirect in that case.

    [DoesNotReturn]
    public static void RedirectTo(this NavigationManager navigationManager, string uri)
    {
        // This works because either:
        // [1] NavigateTo() throws NavigationException, which is handled by the framework as a redirect.
        // [2] NavigateTo() throws some other exception, which gets treated as a normal unhandled exception.
        // [3] NavigateTo() does not throw an exception, meaning we're not rendering from an endpoint, so we throw
        //     an InvalidOperationException to indicate that we can't redirect.
        navigationManager.NavigateTo(uri);
        throw new InvalidOperationException($"Can only redirect when rendering from an endpoint.");
    }

    [DoesNotReturn]
    public static void RedirectTo(this NavigationManager navigationManager, string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        navigationManager.RedirectTo(newUri);
    }

    [DoesNotReturn]
    public static void RedirectToCurrentPage(this NavigationManager navigationManager)
        => navigationManager.RedirectTo(navigationManager.Uri);

    [DoesNotReturn]
    public static void RedirectToCurrentPage(this NavigationManager navigationManager, Dictionary<string, object?> queryParameters)
        => navigationManager.RedirectTo(navigationManager.Uri, queryParameters);
}
