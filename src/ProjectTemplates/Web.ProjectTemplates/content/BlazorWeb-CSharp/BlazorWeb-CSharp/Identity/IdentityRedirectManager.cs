using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace BlazorWeb_CSharp.Identity;

internal sealed class IdentityRedirectManager(
    NavigationManager navigationManager,
    IHttpContextAccessor httpContextAccessor)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder _statusCookieBuilder = new CookieBuilder
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    [DoesNotReturn]
    public void RedirectTo(string uri)
    {
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        // This works because either:
        // [1] NavigateTo() throws NavigationException, which is handled by the framework as a redirect.
        // [2] NavigateTo() throws some other exception, which gets treated as a normal unhandled exception.
        // [3] NavigateTo() does not throw an exception, meaning we're not rendering from an endpoint, so we throw
        //     an InvalidOperationException to indicate that we can't redirect.
        navigationManager.NavigateTo(uri);
        throw new InvalidOperationException($"Can only redirect when rendering from an endpoint.");
    }

    [DoesNotReturn]
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);

        RedirectTo(newUri);
    }

    [DoesNotReturn]
    public void RedirectToWithStatus(string uri, string message)
    {
        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException($"{nameof(RedirectToWithStatus)} requires access to an {nameof(HttpContext)}.");
        httpContext.Response.Cookies.Append(StatusCookieName, message, _statusCookieBuilder.Build(httpContext));

        RedirectTo(uri);
    }

    [DoesNotReturn]
    public void RedirectToCurrentPage()
        => RedirectTo(navigationManager.Uri);

    [DoesNotReturn]
    public void RedirectToCurrentPageWithStatus(string message)
        => RedirectToWithStatus(navigationManager.Uri, message);
}
