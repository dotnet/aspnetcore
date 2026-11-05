// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

internal static class BrowserMessaging
{
    public static async ValueTask PostMessageAsync(IJSRuntime jsRuntime, IJSObjectReference target, JsonElement message)
    {
        // Parse JSON in the browser so payload property names are not interpreted as interop reference markers.
        await using var arguments = await jsRuntime.InvokeAsync<IJSObjectReference>("JSON.parse", $"[{message.GetRawText()}]").ConfigureAwait(false);
        await target.InvokeVoidAsync("postMessage.apply", target, arguments).ConfigureAwait(false);
    }
}
