using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace BlazorWeb_CSharp.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    [FeatureSwitchDefinition("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.EnableThrowNavigationException")]
    private static bool _throwNavigationException =>
        AppContext.TryGetSwitch(_enableThrowNavigationException, out var switchValue) && switchValue;

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
        if (_throwNavigationException)
        {
            // During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
            // So as long as this is called from a statically rendered Identity component, the InvalidOperationException is never thrown.
            throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
        }
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);
}
