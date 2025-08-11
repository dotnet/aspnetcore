// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore;

#nullable enable

internal sealed class DefaultMemoryPoolFactory : IMemoryPoolFactory<byte>, IAsyncDisposable
{
    private readonly IMeterFactory? _meterFactory;
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, bool> _pools = new();
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;
    private readonly ILogger? _logger;

    public DefaultMemoryPoolFactory(IMeterFactory? meterFactory = null, ILogger<DefaultMemoryPoolFactory>? logger = null)
    {
        _meterFactory = meterFactory;
        _logger = logger;
        _timer = new PeriodicTimer(PinnedBlockMemoryPool.DefaultEvictionDelay);
        _timerTask = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    foreach (var pool in _pools.Keys)
                    {
                        pool.PerformEviction();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Error while evicting memory from pools.");
            }
        });
    }

    public MemoryPool<byte> Create()
    {
        var pool = new PinnedBlockMemoryPool(_meterFactory, _logger);

        _pools.TryAdd(pool, true);

        pool.OnPoolDisposed(static (state, self) =>
        {
            ((ConcurrentDictionary<PinnedBlockMemoryPool, bool>)state!).TryRemove(self, out _);
        }, _pools);

        return pool;
    }

    public async ValueTask DisposeAsync()
    {
        _timer.Dispose();
        await _timerTask;
    }
}
