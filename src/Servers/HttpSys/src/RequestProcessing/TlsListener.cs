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
    private static readonly TimeSpan CleanupDelay = TimeSpan.FromSeconds(10);
    private const int CacheSizeLimit = 1_000_000;

    internal TlsListener(ILogger logger, Action<IFeatureCollection, ReadOnlySpan<byte>> tlsClientHelloBytesCallback)
    {
        _logger = logger;
        _tlsClientHelloBytesCallback = tlsClientHelloBytesCallback;

        _cleanupTimer = new PeriodicTimer(CleanupDelay);
        _cleanupTask = CleanupLoopAsync();
    }

    internal void InvokeTlsClientHelloCallback(IFeatureCollection features, Request request)
    {
        if (!request.IsHttps)
        {
            return;
        }

        if (!_connectionTimestamps.TryAdd(request.RawConnectionId, DateTime.UtcNow))
        {
            _connectionTimestamps[request.RawConnectionId] = DateTime.UtcNow; // update TTL
            return;
        }

        _ = request.GetAndInvokeTlsClientHelloCallback(features, _tlsClientHelloBytesCallback);
    }

    private async Task CleanupLoopAsync()
    {
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            try
            {
                var now = DateTime.UtcNow;

                // Remove idle connections
                foreach (var kvp in _connectionTimestamps)
                {
                    if (now - kvp.Value > ConnectionIdleTime)
                    {
                        _connectionTimestamps.TryRemove(kvp.Key, out _);
                    }
                }

                // Evict oldest items if above CacheSizeLimit
                var currentCount = _connectionTimestamps.Count;
                if (currentCount > CacheSizeLimit)
                {
                    var excessCount = currentCount - CacheSizeLimit;

                    // Find the oldest items in a single pass
                    var oldestTimestamps = new SortedSet<KeyValuePair<ulong, DateTime>>(TimeComparer.Instance);

                    foreach (var kvp in _connectionTimestamps)
                    {
                        if (oldestTimestamps.Count < excessCount)
                        {
                            oldestTimestamps.Add(new KeyValuePair<ulong, DateTime>(kvp.Key, kvp.Value));
                        }
                        else if (kvp.Value < oldestTimestamps.Max.Value)
                        {
                            oldestTimestamps.Remove(oldestTimestamps.Max);
                            oldestTimestamps.Add(new KeyValuePair<ulong, DateTime>(kvp.Key, kvp.Value));
                        }
                    }

                    // Remove the oldest keys
                    foreach (var item in oldestTimestamps)
                    {
                        _connectionTimestamps.TryRemove(item.Key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.CleanupClosedConnectionError(_logger, ex);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _cleanupTask.Wait();
    }

    private sealed class TimeComparer : IComparer<KeyValuePair<ulong, DateTime>>
    {
        public static TimeComparer Instance { get; } = new TimeComparer();

        public int Compare(KeyValuePair<ulong, DateTime> x, KeyValuePair<ulong, DateTime> y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }
}
