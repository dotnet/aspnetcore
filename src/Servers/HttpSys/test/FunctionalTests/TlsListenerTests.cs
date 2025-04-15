// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.HttpSys.RequestProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using static Microsoft.AspNetCore.Server.HttpSys.HttpSysOptions;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests;

public class TlsListenerTests
{
    [Fact]
    public void AddsAndUpdatesConnectionTimestamps()
    {
        // Arrange
        var logger = Mock.Of<ILogger>();
        var timeProvider = new FakeTimeProvider();
        var callbackInvoked = false;
        var tlsListener = new TlsListener(logger, (_, __) => { callbackInvoked = true; }, timeProvider);

        var features = Mock.Of<IFeatureCollection>();

        // Act
        tlsListener.InvokeTlsClientHelloCallback(connectionId: 1UL, features,
            invokeTlsClientHelloCallback: (f, cb) => { cb(f, ReadOnlySpan<byte>.Empty); return true; });

        var originalTime = timeProvider.GetUtcNow();

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal(originalTime, Assert.Single(tlsListener.ConnectionTimeStamps).Value);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        callbackInvoked = false;
        // Update the timestamp
        tlsListener.InvokeTlsClientHelloCallback(connectionId: 1UL, features,
            invokeTlsClientHelloCallback: (f, cb) => { cb(f, ReadOnlySpan<byte>.Empty); return true; });

        // Callback should not be invoked again and the timestamp should be updated
        Assert.False(callbackInvoked);
        Assert.Equal(timeProvider.GetUtcNow(), Assert.Single(tlsListener.ConnectionTimeStamps).Value);
        Assert.NotEqual(originalTime, timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task RemovesIdleConnections()
    {
        // Arrange
        var logger = Mock.Of<ILogger>();
        var timeProvider = new FakeTimeProvider();
        using var tlsListener = new TlsListener(logger, (_, __) => { }, timeProvider);

        var features = Mock.Of<IFeatureCollection>();

        bool InvokeCallback(IFeatureCollection f, TlsClientHelloCallback cb)
        {
            cb(f, ReadOnlySpan<byte>.Empty);
            return true;
        }

        // Act
        tlsListener.InvokeTlsClientHelloCallback(connectionId: 1UL, features, InvokeCallback);

        // 1 less minute than the idle time cleanup
        timeProvider.Advance(TimeSpan.FromMinutes(4));
        Assert.Single(tlsListener.ConnectionTimeStamps);

        tlsListener.InvokeTlsClientHelloCallback(connectionId: 2UL, features, InvokeCallback);
        Assert.Equal(2, tlsListener.ConnectionTimeStamps.Count);

        // With the previous 4 minutes, this should be 5 minutes and remove the first connection
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        var timeout = TimeSpan.FromSeconds(5);
        while (timeout > TimeSpan.Zero)
        {
            // Wait for the cleanup loop to run
            if (tlsListener.ConnectionTimeStamps.Count == 1)
            {
                break;
            }
            timeout -= TimeSpan.FromMilliseconds(100);
            await Task.Delay(100);
        }

        // Assert
        Assert.Single(tlsListener.ConnectionTimeStamps);
        Assert.Contains(2UL, tlsListener.ConnectionTimeStamps.Keys);
    }

    [Fact]
    public async Task EvictsOldestConnectionsWhenExceedingCacheSizeLimit()
    {
        // Arrange
        var logger = Mock.Of<ILogger>();
        var timeProvider = new FakeTimeProvider();
        var tlsListener = new TlsListener(logger, (_, __) => { }, timeProvider);
        var features = Mock.Of<IFeatureCollection>();

        ulong i = 0;
        for (; i < (ulong)tlsListener.CacheSizeLimit; i++)
        {
            tlsListener.InvokeTlsClientHelloCallback(i, features, (f, cb) => { cb(f, ReadOnlySpan<byte>.Empty); return true; });
        }

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        for (; i < (ulong)tlsListener.CacheSizeLimit + 3; i++)
        {
            tlsListener.InvokeTlsClientHelloCallback(i, features, (f, cb) => { cb(f, ReadOnlySpan<byte>.Empty); return true; });
        }

        // 'touch' first connection to update its timestamp
        tlsListener.InvokeTlsClientHelloCallback(0, features, (f, cb) => { cb(f, ReadOnlySpan<byte>.Empty); return true; });

        // Make sure the cleanup loop has run to evict items since we're above the cache size limit
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        var timeout = TimeSpan.FromSeconds(5);
        while (timeout > TimeSpan.Zero)
        {
            // Wait for the cleanup loop to run
            if (tlsListener.ConnectionTimeStamps.Count == tlsListener.CacheSizeLimit)
            {
                break;
            }
            timeout -= TimeSpan.FromMilliseconds(100);
            await Task.Delay(100);
        }

        Assert.Equal(tlsListener.CacheSizeLimit, tlsListener.ConnectionTimeStamps.Count);
        Assert.Contains(0UL, tlsListener.ConnectionTimeStamps.Keys);
        // 3 newest connections should be present
        Assert.Contains(i - 1, tlsListener.ConnectionTimeStamps.Keys);
        Assert.Contains(i - 2, tlsListener.ConnectionTimeStamps.Keys);
        Assert.Contains(i - 3, tlsListener.ConnectionTimeStamps.Keys);
    }
}
