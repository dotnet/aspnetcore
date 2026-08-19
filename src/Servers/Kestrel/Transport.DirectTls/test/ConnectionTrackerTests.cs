// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for <see cref="ConnectionTracker"/> - the listener-level in-flight cap that the pumps consult
/// to reject a freshly accepted connection (before the TLS handshake) once the configured maximum number of
/// simultaneously handshaking or ready-but-unaccepted connections is reached.
/// </summary>
public class ConnectionTrackerTests
{
    [Fact]
    public void Unlimited_AlwaysAdmits_AndNeverCounts()
    {
        var tracker = ConnectionTracker.Unlimited;

        Assert.True(tracker.TryAcquireHandshake());
        Assert.True(tracker.TryAcquireHandshake());
        Assert.Equal(0, tracker.HandshakeCount);

        tracker.ReleaseHandshake();
        Assert.Equal(0, tracker.HandshakeCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-5L)]
    public void Disabled_WhenLimitNullOrNonPositive_AlwaysAdmits(long? maxHandshakes)
    {
        var tracker = new ConnectionTracker(maxHandshakes);

        for (var i = 0; i < 1000; i++)
        {
            Assert.True(tracker.TryAcquireHandshake());
        }

        // Disabled trackers never count, so acquire/release leave HandshakeCount at zero.
        Assert.Equal(0, tracker.HandshakeCount);
        tracker.ReleaseHandshake();
        Assert.Equal(0, tracker.HandshakeCount);
    }

    [Fact]
    public void TryAcquireHandshake_AdmitsUpToCap_ThenRejects()
    {
        var tracker = new ConnectionTracker(maxHandshakes: 3);

        Assert.True(tracker.TryAcquireHandshake());
        Assert.True(tracker.TryAcquireHandshake());
        Assert.True(tracker.TryAcquireHandshake());
        Assert.False(tracker.TryAcquireHandshake());
        Assert.Equal(3, tracker.HandshakeCount);
    }

    [Fact]
    public void TryAcquireHandshake_CapOfOne_AdmitsExactlyOne()
    {
        var tracker = new ConnectionTracker(maxHandshakes: 1);

        Assert.True(tracker.TryAcquireHandshake());
        Assert.False(tracker.TryAcquireHandshake());
        Assert.Equal(1, tracker.HandshakeCount);
    }

    [Fact]
    public void ReleaseHandshake_FreesSlot_SoNextAcquireSucceeds()
    {
        var tracker = new ConnectionTracker(maxHandshakes: 1);
        Assert.True(tracker.TryAcquireHandshake());
        Assert.False(tracker.TryAcquireHandshake());

        tracker.ReleaseHandshake();

        Assert.Equal(0, tracker.HandshakeCount);
        Assert.True(tracker.TryAcquireHandshake());
        Assert.Equal(1, tracker.HandshakeCount);
    }

    [Fact]
    public void HandshakeCount_ReflectsAcquiresAndReleases()
    {
        var tracker = new ConnectionTracker(maxHandshakes: 5);

        tracker.TryAcquireHandshake();
        tracker.TryAcquireHandshake();
        Assert.Equal(2, tracker.HandshakeCount);

        tracker.ReleaseHandshake();
        Assert.Equal(1, tracker.HandshakeCount);

        tracker.ReleaseHandshake();
        Assert.Equal(0, tracker.HandshakeCount);
    }

    [Fact]
    public void TryAcquireHandshake_NeverExceedsCap_UnderConcurrentAcquire()
    {
        const int cap = 50;
        const int contenders = 32;
        const int attemptsPerThread = 1000;

        var tracker = new ConnectionTracker(cap);
        var granted = new ConcurrentBag<bool>();
        using var start = new ManualResetEventSlim(false);

        var threads = new Thread[contenders];
        for (var i = 0; i < contenders; i++)
        {
            threads[i] = new Thread(() =>
            {
                start.Wait();
                for (var attempt = 0; attempt < attemptsPerThread; attempt++)
                {
                    if (tracker.TryAcquireHandshake())
                    {
                        granted.Add(true);
                    }
                }
            });
            threads[i].Start();
        }

        // Release the threads simultaneously to maximize contention on the shared counter.
        start.Set();
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Nothing is released while the threads run, so the number of successful acquires - and the final
        // count - must be exactly the cap, never more, no matter how the increments interleaved.
        Assert.Equal(cap, granted.Count);
        Assert.Equal(cap, tracker.HandshakeCount);
    }

    [Fact]
    public void AcquireRelease_StaysBounded_UnderConcurrentChurn()
    {
        const int cap = 8;
        const int workers = 16;
        const int iterationsPerWorker = 5000;

        var tracker = new ConnectionTracker(cap);
        long held = 0;
        long observedMax = 0;

        Parallel.For(0, workers, _ =>
        {
            for (var i = 0; i < iterationsPerWorker; i++)
            {
                if (tracker.TryAcquireHandshake())
                {
                    // Count the slots actually held right now; admission is exact, so this never exceeds the cap
                    // even though the tracker's raw counter can transiently overshoot while a rejected caller
                    // increments then backs out.
                    var current = Interlocked.Increment(ref held);
                    InterlockedMax(ref observedMax, current);
                    Interlocked.Decrement(ref held);
                    tracker.ReleaseHandshake();
                }
            }
        });

        Assert.True(observedMax <= cap, $"Observed in-flight count {observedMax} exceeded cap {cap}.");
        Assert.Equal(0, tracker.HandshakeCount);
    }

    private static void InterlockedMax(ref long location, long value)
    {
        var current = Interlocked.Read(ref location);
        while (value > current)
        {
            var prev = Interlocked.CompareExchange(ref location, value, current);
            if (prev == current)
            {
                return;
            }

            current = prev;
        }
    }
}
