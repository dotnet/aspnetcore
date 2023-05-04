// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class HeartbeatManager : TimeProvider, IHeartbeatHandler
{
    private readonly ConnectionManager _connectionManager;
    private readonly Action<KestrelConnection> _walkCallback;
    private long _nowTicks;

    public HeartbeatManager(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _walkCallback = WalkCallback;
    }

    public override DateTimeOffset GetUtcNow() => new(GetTimestamp(), TimeSpan.Zero);

    public override long GetTimestamp() => Volatile.Read(ref _nowTicks);

    public void OnHeartbeat(DateTimeOffset now)
    {
        Volatile.Write(ref _nowTicks, now.Ticks);

        _connectionManager.Walk(_walkCallback);
    }

    private void WalkCallback(KestrelConnection connection)
    {
        connection.TickHeartbeat();
    }
}
