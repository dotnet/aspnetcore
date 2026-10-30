// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the screen that the browser window is shown on.
/// </summary>
public sealed class Screen : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal Screen(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the width of the screen, in CSS pixels, without the space that the operating system
    /// takes for its own user interface, such as a taskbar.
    /// </summary>
    /// <returns>The available width, in CSS pixels.</returns>
    public async ValueTask<int> GetAvailWidthAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<int>("availWidth").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the height of the screen, in CSS pixels, without the space that the operating system
    /// takes for its own user interface, such as a taskbar.
    /// </summary>
    /// <returns>The available height, in CSS pixels.</returns>
    public async ValueTask<int> GetAvailHeightAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<int>("availHeight").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the width of the screen, in CSS pixels.
    /// </summary>
    /// <returns>The width, in CSS pixels.</returns>
    public async ValueTask<int> GetWidthAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<int>("width").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the height of the screen, in CSS pixels.
    /// </summary>
    /// <returns>The height, in CSS pixels.</returns>
    public async ValueTask<int> GetHeightAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<int>("height").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the number of bits used for the color of a pixel on the screen, not counting the alpha channel.
    /// </summary>
    /// <returns>The number of bits, such as 24.</returns>
    public async ValueTask<uint> GetColorDepthAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<uint>("colorDepth").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the number of bits used for the color of a pixel on the screen, not counting the alpha channel.
    /// </summary>
    /// <remarks>
    /// Browsers return the same value as <see cref="GetColorDepthAsync"/>.
    /// </remarks>
    /// <returns>The number of bits, such as 24.</returns>
    public async ValueTask<uint> GetPixelDepthAsync()
    {
        var screen = await GetReferenceAsync().ConfigureAwait(false);

        return await screen.GetValueAsync<uint>("pixelDepth").ConfigureAwait(false);
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Don't wait for a pending acquisition. It could hold up scope disposal until the JS interop timeout.
        if (_referenceTask is { IsCompletedSuccessfully: true } referenceTask)
        {
            await referenceTask.Result.DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_referenceTask is null || _referenceTask.IsFaulted || _referenceTask.IsCanceled)
        {
            _referenceTask = _jsRuntime.GetValueAsync<IJSObjectReference>("screen").AsTask();
        }

        return _referenceTask;
    }
}
