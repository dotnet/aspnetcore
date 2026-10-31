// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Holds a browser resource (script, stylesheet, fetch, etc.) by intercepting
/// its Playwright route. The resource request is paused until the lock is
/// disposed or explicitly released.
/// </summary>
/// <remarks>
/// <para>
/// Create a lock before the navigation that triggers the resource request,
/// then call <see cref="WaitForRequestAsync"/> after navigating:
/// </para>
/// <code>
/// await using var blazorScript = await ResourceLock.CreateAsync(
///     page, new Regex(@"blazor\.web.*\.js"));
///
/// await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Commit });
/// await blazorScript.WaitForRequestAsync();
///
/// // Resource is held — Blazor hasn't started
/// await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
/// </code>
/// </remarks>
public sealed class ResourceLock : IAsyncDisposable
{
    private readonly IPage _page;
    private readonly Regex _urlPattern;
    private readonly TaskCompletionSource _requestedTcs = new();
    private IRoute? _heldRoute;
    private int _captured;
    private bool _released;

    ResourceLock(IPage page, Regex urlPattern)
    {
        _page = page;
        _urlPattern = urlPattern;
    }

    /// <summary>
    /// Sets up route interception for requests matching the given URL pattern.
    /// Must be called before the navigation that triggers the resource request.
    /// </summary>
    /// <param name="page">The page to intercept routes on.</param>
    /// <param name="urlPattern">A regex pattern matching the URL(s) to intercept.</param>
    /// <returns>A <see cref="ResourceLock"/> that holds the first matching request.</returns>
    public static async Task<ResourceLock> CreateAsync(IPage page, Regex urlPattern)
    {
        var resourceLock = new ResourceLock(page, urlPattern);
        await page.RouteAsync(urlPattern, async route =>
        {
            if (Interlocked.CompareExchange(ref resourceLock._captured, 1, 0) == 0)
            {
                resourceLock._heldRoute = route;
                resourceLock._requestedTcs.TrySetResult();
            }
            else
            {
                await route.ContinueAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
        return resourceLock;
    }

    /// <summary>
    /// Waits until the browser actually requests the intercepted resource.
    /// Call this after the navigation that triggers the request.
    /// </summary>
    public Task WaitForRequestAsync() => _requestedTcs.Task;

    /// <summary>
    /// Explicitly releases the held route, allowing the request to continue.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    public async Task ReleaseAsync()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        if (_heldRoute is not null)
        {
            await _heldRoute.ContinueAsync().ConfigureAwait(false);
        }
        await _page.UnrouteAsync(_urlPattern).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
}
