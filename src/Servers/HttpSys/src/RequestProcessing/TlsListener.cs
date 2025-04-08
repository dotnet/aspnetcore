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

    private readonly PeriodicTimer _cleanupTimer;
    private readonly Task _cleanupTask;

    private static readonly TimeSpan ConnectionIdleTime = TimeSpan.FromMinutes(5);

    internal TlsListener(ILogger logger, Action<IFeatureCollection, ReadOnlySpan<byte>> tlsClientHelloBytesCallback)
    {
        _logger = logger;
        _tlsClientHelloBytesCallback = tlsClientHelloBytesCallback;

        _cleanupTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        _cleanupTask = CleanupLoopAsync();
    }

    internal void InvokeTlsClientHelloCallback(IFeatureCollection features, Request request)
    {
        if (!request.IsHttps)
        {
            return;
        }

        if (_connectionTimestamps.ContainsKey(request.RawConnectionId))
        {
            _connectionTimestamps[request.RawConnectionId] = DateTime.UtcNow;
            return;
        }

        var success = request.GetAndInvokeTlsClientHelloCallback(features, _tlsClientHelloBytesCallback);
        if (success)
        {
            _connectionTimestamps[request.RawConnectionId] = DateTime.UtcNow;
        }
    }

    private async Task CleanupLoopAsync()
    {
        try
        {
            while (await _cleanupTimer.WaitForNextTickAsync())
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _connectionTimestamps)
                {
                    if (now - kvp.Value > ConnectionIdleTime)
                    {
                        _connectionTimestamps.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.CleanupClosedConnectionError(_logger, ex);
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _cleanupTask.Wait();
    }
}
