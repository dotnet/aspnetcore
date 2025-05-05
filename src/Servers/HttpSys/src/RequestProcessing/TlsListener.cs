// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys.RequestProcessing;

internal sealed partial class TlsListener : IDisposable
{
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _connectionTimestamps = new();
    private readonly Action<IFeatureCollection, ReadOnlySpan<byte>> _tlsClientHelloBytesCallback;
    private readonly ILogger _logger;

    private readonly PeriodicTimer _cleanupTimer;
    private readonly Task _cleanupTask;
    private readonly TimeProvider _timeProvider;

    private readonly TimeSpan ConnectionIdleTime = TimeSpan.FromMinutes(5);
    private readonly TimeSpan CleanupDelay = TimeSpan.FromSeconds(10);
    internal readonly int CacheSizeLimit = 1_000_000;

    // Internal for testing purposes
    internal ReadOnlyDictionary<ulong, DateTimeOffset> ConnectionTimeStamps => _connectionTimestamps.AsReadOnly();

    internal TlsListener(ILogger logger, Action<IFeatureCollection, ReadOnlySpan<byte>> tlsClientHelloBytesCallback, TimeProvider? timeProvider = null)
    {
        if (AppContext.GetData("Microsoft.AspNetCore.Server.HttpSys.TlsListener.CacheSizeLimit") is int limit)
        {
            CacheSizeLimit = limit;
        }

        if (AppContext.GetData("Microsoft.AspNetCore.Server.HttpSys.TlsListener.ConnectionIdleTime") is int idleTime)
        {
            ConnectionIdleTime = TimeSpan.FromSeconds(idleTime);
        }

        if (AppContext.GetData("Microsoft.AspNetCore.Server.HttpSys.TlsListener.CleanupDelay") is int cleanupDelay)
        {
            CleanupDelay = TimeSpan.FromSeconds(cleanupDelay);
        }

        _logger = logger;
        _tlsClientHelloBytesCallback = tlsClientHelloBytesCallback;

        _timeProvider = timeProvider ?? TimeProvider.System;
        _cleanupTimer = new PeriodicTimer(CleanupDelay, _timeProvider);
        _cleanupTask = CleanupLoopAsync();
    }

    // Method looks weird because we want it to be testable by not directly requiring a Request object
    internal void InvokeTlsClientHelloCallback(ulong connectionId, IFeatureCollection features,
        Func<IFeatureCollection, Action<IFeatureCollection, ReadOnlySpan<byte>>, bool> invokeTlsClientHelloCallback)
    {
        if (!_connectionTimestamps.TryAdd(connectionId, _timeProvider.GetUtcNow()))
        {
            // update TTL
            _connectionTimestamps[connectionId] = _timeProvider.GetUtcNow();
            return;
        }

        _ = invokeTlsClientHelloCallback(features, _tlsClientHelloBytesCallback);
    }

    internal async Task CleanupLoopAsync()
    {
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            try
            {
                var now = _timeProvider.GetUtcNow();

                // Remove idle connections
                foreach (var kvp in _connectionTimestamps)
                {
                    if (now - kvp.Value >= ConnectionIdleTime)
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
                    var oldestTimestamps = new SortedSet<KeyValuePair<ulong, DateTimeOffset>>(TimeComparer.Instance);

                    foreach (var kvp in _connectionTimestamps)
                    {
                        if (oldestTimestamps.Count < excessCount)
                        {
                            oldestTimestamps.Add(new KeyValuePair<ulong, DateTimeOffset>(kvp.Key, kvp.Value));
                        }
                        else if (kvp.Value < oldestTimestamps.Max.Value)
                        {
                            oldestTimestamps.Remove(oldestTimestamps.Max);
                            oldestTimestamps.Add(new KeyValuePair<ulong, DateTimeOffset>(kvp.Key, kvp.Value));
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

    private sealed class TimeComparer : IComparer<KeyValuePair<ulong, DateTimeOffset>>
    {
        public static TimeComparer Instance { get; } = new TimeComparer();

        public int Compare(KeyValuePair<ulong, DateTimeOffset> x, KeyValuePair<ulong, DateTimeOffset> y)
        {
            // Compare timestamps first
            int timestampComparison = x.Value.CompareTo(y.Value);
            if (timestampComparison != 0)
            {
                return timestampComparison;
            }

            // Use the key as a tiebreaker to ensure uniqueness
            return x.Key.CompareTo(y.Key);
        }
    }
}
