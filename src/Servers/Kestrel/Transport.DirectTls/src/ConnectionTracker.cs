// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Listener-level counters bounding the pre-Kestrel work a single listener will do. One tracker is shared by
/// every pump of a listener, so its counters cap that listener across all its pump threads. Today it tracks
/// in-flight TLS handshakes (see <see cref="TryAcquireHandshake"/>); the type is intentionally named generally
/// so additional per-listener limits can be added as further acquire/release pairs.
/// <para>
/// On this transport the TLS handshake runs on the pump thread <em>before</em> the connection is surfaced to
/// Kestrel, so Kestrel's own <c>MaxConcurrentConnections</c> limit - which only counts accepted connections -
/// cannot bound a handshake flood. The handshake counter closes that gap: a pump calls
/// <see cref="TryAcquireHandshake"/> for a freshly accepted connection and, if the cap is already reached,
/// rejects it (closes the fd) before starting the handshake. Each admitted connection frees its slot via
/// <see cref="ReleaseHandshake"/> when it is dropped during handshake or handed to Kestrel. This mirrors
/// Kestrel's <c>MaxConcurrentConnections</c> reject behavior, but pre-handshake so a flood cannot spend server
/// crypto.
/// </para>
/// </summary>
internal sealed class ConnectionTracker
{
    /// <summary>
    /// A shared tracker with no configured limits. Every acquire succeeds and every release is a no-op, so a
    /// disabled tracker holds no state and is safe to share across all listeners and pumps. Used as the default
    /// when no cap is configured, avoiding a null field and per-connection null checks on the accept path.
    /// </summary>
    public static readonly ConnectionTracker Unlimited = new(maxHandshakes: null);

    private readonly bool _handshakeLimitEnabled;
    private readonly long _maxHandshakes;
    private long _handshakeCount;

    /// <param name="maxHandshakes">
    /// The maximum number of simultaneously in-flight handshakes, or <see langword="null"/> (or a non-positive
    /// value) to leave handshakes unlimited, in which case <see cref="TryAcquireHandshake"/> and
    /// <see cref="ReleaseHandshake"/> are no-ops.
    /// </param>
    public ConnectionTracker(long? maxHandshakes)
    {
        _handshakeLimitEnabled = maxHandshakes is > 0;
        _maxHandshakes = maxHandshakes ?? 0;
    }

    /// <summary>
    /// The number of connections currently counted against the handshake cap (handshaking or sitting in the
    /// ready channel awaiting <c>AcceptAsync</c>). Always 0 when the handshake limit is disabled. For tests and
    /// diagnostics.
    /// </summary>
    internal long HandshakeCount => Interlocked.Read(ref _handshakeCount);

    /// <summary>
    /// Tries to admit a freshly accepted connection into the handshake stage. Returns <see langword="true"/>
    /// and reserves a slot when the in-flight handshake count is below the cap; returns <see langword="false"/>
    /// when the cap is reached, in which case the caller must reject the connection. Always returns
    /// <see langword="true"/> when the handshake limit is disabled. Lock-free; safe to call concurrently from
    /// every pump.
    /// </summary>
    public bool TryAcquireHandshake()
    {
        if (!_handshakeLimitEnabled)
        {
            return true;
        }

        // Reserve a slot optimistically, then hand it straight back if that put us over the cap. Each concurrent
        // caller sees a distinct post-increment value, so at most _maxHandshakes callers ever observe a value
        // within the cap - the admission limit stays exact even though a rejected caller briefly overshoots it.
        if (Interlocked.Increment(ref _handshakeCount) <= _maxHandshakes)
        {
            return true;
        }

        Interlocked.Decrement(ref _handshakeCount);
        return false;
    }

    /// <summary>
    /// Frees a slot reserved by a prior successful <see cref="TryAcquireHandshake"/>. Called once per admitted
    /// connection when it leaves the pre-Kestrel pipeline - dropped during handshake or handed to Kestrel via
    /// <c>AcceptAsync</c>. A no-op when the handshake limit is disabled.
    /// </summary>
    public void ReleaseHandshake()
    {
        if (!_handshakeLimitEnabled)
        {
            return;
        }

        var updated = Interlocked.Decrement(ref _handshakeCount);
        Debug.Assert(updated >= 0, "ConnectionTracker released more handshake slots than were acquired.");
    }
}
