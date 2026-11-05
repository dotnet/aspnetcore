// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates caller-owned browser message channels.
/// </summary>
public sealed class MessageChannelConstructor
{
    private readonly IJSRuntime _jsRuntime;

    internal MessageChannelConstructor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates a channel with two connected message ports.
    /// </summary>
    /// <returns>The created channel, which the caller must dispose.</returns>
    public async ValueTask<MessageChannel> CreateAsync()
    {
        var reference = await _jsRuntime.InvokeConstructorAsync("MessageChannel").ConfigureAwait(false);

        return new MessageChannel(_jsRuntime, reference);
    }
}
