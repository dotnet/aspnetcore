// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates caller-owned browser broadcast channels.
/// </summary>
public sealed class BroadcastChannelConstructor
{
    private readonly FeatureContext _context;

    internal BroadcastChannelConstructor(FeatureContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a broadcast channel with the specified name.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <returns>The created channel, which the caller must dispose.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public async ValueTask<BroadcastChannel> CreateAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var reference = await _context.JSRuntime.InvokeConstructorAsync("BroadcastChannel", name).ConfigureAwait(false);

        return new BroadcastChannel(_context, reference);
    }
}
