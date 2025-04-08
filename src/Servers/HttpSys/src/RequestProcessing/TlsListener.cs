// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys.RequestProcessing;

internal sealed partial class TlsListener : IDisposable
{
    private readonly ConcurrentDictionary<ulong, DateTime> _connectionTimestamps = new();
    private readonly Action<IFeatureCollection, ReadOnlySpan<byte>> _tlsClientHelloBytesCallback;

    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _cleanupTask;

    private static readonly TimeSpan ConnectionIdleTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(30);

    internal TlsListener(ILogger logger, Action<IFeatureCollection, ReadOnlySpan<byte>> tlsClientHelloBytesCallback)
    {
        _logger = logger;
        _tlsClientHelloBytesCallback = tlsClientHelloBytesCallback;

        _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
    }

    internal void InvokeTlsClientHelloCallback(IFeatureCollection features, Request request)
    {
        if (!request.IsHttps)
        {
            return;
        }

        if (!_connectionTimestamps.TryAdd(request.RawConnectionId, DateTime.UtcNow))
        {
            // update the TTL
            _connectionTimestamps[request.RawConnectionId] = DateTime.UtcNow;
            return;
        }

        var success = request.GetAndInvokeTlsClientHelloCallback(features, _tlsClientHelloBytesCallback);
        if (success)
        {
            _connectionTimestamps[request.RawConnectionId] = DateTime.UtcNow;
        }
    }

    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {            
            var now = DateTime.UtcNow;
            foreach (var kvp in _connectionTimestamps)
            {
                if (now - kvp.Value > ConnectionIdleTime)
                {
                    _connectionTimestamps.TryRemove(kvp.Key, out _);
                }
            }

            try
            {
                await Task.Delay(CleanupInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _cleanupTask.Wait();
        }
        catch
        {
            // ignore
        }
        _cts.Dispose();
    }
}
