// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class ConnectionLimitMiddleware<T> where T : BaseConnectionContext
{
    private readonly Func<T, Task> _next;
    private readonly ResourceCounter _concurrentConnectionCounter;
    private readonly KestrelTrace _trace;

    public ConnectionLimitMiddleware(Func<T, Task> next, long connectionLimit, KestrelTrace trace)
        : this(next, ResourceCounter.Quota(connectionLimit), trace)
    {
    }

    // For Testing
    internal ConnectionLimitMiddleware(Func<T, Task> next, ResourceCounter concurrentConnectionCounter, KestrelTrace trace)
    {
        _next = next;
        _concurrentConnectionCounter = concurrentConnectionCounter;
        _trace = trace;
    }

    public async Task OnConnectionAsync(T connection)
    {
        if (!_concurrentConnectionCounter.TryLockOne())
        {
            KestrelEventSource.Log.ConnectionRejected(connection.ConnectionId);
            _trace.ConnectionRejected(connection.ConnectionId);
            await connection.DisposeAsync();
            return;
        }

        var releasor = new ConnectionReleasor(_concurrentConnectionCounter);

        try
        {
            connection.Features.Set<IDecrementConcurrentConnectionCountFeature>(releasor);
            await _next(connection);
        }
        finally
        {
            releasor.ReleaseConnection();
        }
    }

    private sealed class ConnectionReleasor : IDecrementConcurrentConnectionCountFeature
    {
        private readonly ResourceCounter _concurrentConnectionCounter;
        private bool _connectionReleased;

        public ConnectionReleasor(ResourceCounter normalConnectionCounter)
        {
            _concurrentConnectionCounter = normalConnectionCounter;
        }

        public void ReleaseConnection()
        {
            if (!_connectionReleased)
            {
                _connectionReleased = true;
                _concurrentConnectionCounter.ReleaseOne();
            }
        }
    }
}
