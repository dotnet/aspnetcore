// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Connections;

namespace System.Buffers;

internal sealed class DefaultMemoryPoolFactory : IMemoryPoolFactory<byte>, IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, PinnedBlockMemoryPool> _pools = new();
    private readonly PeriodicTimer _timer;

    public DefaultMemoryPoolFactory(IMeterFactory? meterFactory = null)
    {
        _meterFactory = meterFactory ?? NoopMeterFactory.Instance;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        _ = Task.Run(async () =>
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
                Debug.WriteLine(ex);
            }
        });
    }

    public MemoryPool<byte> Create()
    {
        var pool = new PinnedBlockMemoryPool(_meterFactory);
        pool.DisposeCallback = (self) =>
        {
            _pools.TryRemove(self, out _);
        };

        _pools.TryAdd(pool, pool);

        return pool;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private sealed class NoopMeterFactory : IMeterFactory
    {
        public static NoopMeterFactory Instance = new NoopMeterFactory();

        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose()
        {
        }
    }
}
