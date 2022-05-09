// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class HeartbeatManager : IHeartbeatHandler, ISystemClock
{
    private readonly ConnectionManager _connectionManager;
    private readonly Action<KestrelConnection> _walkCallback;
    private DateTimeOffset _now;
    private long _nowTicks;

    public HeartbeatManager(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _walkCallback = WalkCallback;
    }

    public DateTimeOffset UtcNow => new DateTimeOffset(UtcNowTicks, TimeSpan.Zero);

    public long UtcNowTicks => Volatile.Read(ref _nowTicks);

    public DateTimeOffset UtcNowUnsynchronized => _now;

    public void OnHeartbeat(DateTimeOffset now)
    {
        _now = now;
        Volatile.Write(ref _nowTicks, now.Ticks);

        _connectionManager.Walk(_walkCallback);
    }

    private void WalkCallback(KestrelConnection connection)
    {
        connection.TickHeartbeat();
    }
}
