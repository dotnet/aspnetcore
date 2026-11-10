// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates the <see cref="AbortLink"/> instances of the scope, and releases those still open when the scope ends.
/// </summary>
/// <remarks>
/// Call it on the scope's synchronization context. The list of open links has no lock.
/// </remarks>
internal sealed partial class AbortLinkFactory : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger _logger;
    private readonly List<AbortLink> _links = [];
    private bool _disposed;

    public AbortLinkFactory(IJSRuntime jsRuntime, ILogger logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Creates a link for <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken">The token that aborts the browser operation.</param>
    /// <returns>
    /// The link, or <see langword="null"/> if <paramref name="cancellationToken"/> cannot be canceled.
    /// </returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is already canceled.</exception>
    /// <exception cref="ObjectDisposedException">The scope has ended.</exception>
    public async ValueTask<AbortLink?> CreateAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!cancellationToken.CanBeCanceled)
        {
            return null;
        }

        var controller = await _jsRuntime.InvokeConstructorAsync("AbortController");
        IJSObjectReference signal;
        try
        {
            signal = await controller.GetValueAsync<IJSObjectReference>("signal");
        }
        catch
        {
            await controller.DisposeAsync();
            throw;
        }

        var link = new AbortLink(this, controller, signal, _logger);
        if (cancellationToken.IsCancellationRequested)
        {
            await link.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Scope disposal can run while the controller is created, and it has not seen this link.
        if (_disposed)
        {
            await link.DisposeAsync();
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        _links.Add(link);
        link.AbortWhenCanceled(cancellationToken);

        return link;
    }

    internal void Remove(AbortLink link) => _links.Remove(link);

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stops a token that outlives the scope from keeping the scope alive through an operation that never ended.
        foreach (var link in _links.ToArray())
        {
            try
            {
                await link.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.ReleaseFailed(_logger, ex);
            }
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "A browser operation could not be aborted.", EventName = "AbortFailed")]
        public static partial void AbortFailed(ILogger logger, Exception exception);

        [LoggerMessage(2, LogLevel.Warning, "An abort link could not be released when the scope ended.", EventName = "ReleaseFailed")]
        public static partial void ReleaseFailed(ILogger logger, Exception exception);
    }
}
