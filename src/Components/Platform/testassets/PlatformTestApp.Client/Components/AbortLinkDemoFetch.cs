// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Platform;
using Microsoft.JSInterop;

namespace PlatformTestApp.Client.Components;

/// <summary>
/// Calls <c>fetch</c> the way a family with an <c>AbortSignal</c> does, for the abort link demo.
/// </summary>
internal sealed class AbortLinkDemoFetch
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AbortLinkFactory _abortLinks;

    public AbortLinkDemoFetch(IJSRuntime jsRuntime, AbortLinkFactory abortLinks)
    {
        _jsRuntime = jsRuntime;
        _abortLinks = abortLinks;
    }

    public async Task<string> FetchTextAsync(string url, CancellationToken cancellationToken, Action? onBodyReadStarted = null)
    {
        var link = await _abortLinks.CreateAsync(cancellationToken);
        try
        {
            // CancellationToken.None also turns off the interop timeout. Only the link cancels the request.
            await using var response = await _jsRuntime.InvokeAsync<IJSObjectReference>("fetch", CancellationToken.None, [url, new { signal = link?.Signal }]);
            // The browser may have answered just before the abort reached it.
            cancellationToken.ThrowIfCancellationRequested();

            var text = response.InvokeAsync<string>("text", CancellationToken.None, []);
            onBodyReadStarted?.Invoke();

            return await text;
        }
        catch (JSException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw AbortLink.CreateCanceledException(ex, cancellationToken);
        }
        finally
        {
            if (link is not null)
            {
                await link.DisposeAsync();
            }
        }
    }
}
