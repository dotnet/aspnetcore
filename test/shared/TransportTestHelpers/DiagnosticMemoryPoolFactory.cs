// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class DiagnosticMemoryPoolFactory
    {
        private readonly bool _allowLateReturn;

        private readonly bool _rentTracking;

        private readonly List<DiagnosticMemoryPool> _pools;

        public DiagnosticMemoryPoolFactory(bool allowLateReturn = false, bool rentTracking = false)
        {
            _allowLateReturn = allowLateReturn;
            _rentTracking = rentTracking;
            _pools = new List<DiagnosticMemoryPool>();
        }

        public MemoryPool<byte> Create()
        {
            lock (_pools)
            {
                var pool = new DiagnosticMemoryPool(KestrelMemoryPool.CreateSlabMemoryPool(), _allowLateReturn, _rentTracking);
                _pools.Add(pool);
                return pool;
            }
        }

        public Task WhenAllBlocksReturned(TimeSpan span)
        {
            lock (_pools)
            {
                return Task.WhenAll(_pools.Select(p=>p.WhenAllBlocksReturnedAsync(span)));
            }
        }
    }
}