// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.MemoryPool;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    /// <summary>
    /// A <see cref="IRazorBufferScope"/> that uses pooled memory.
    /// </summary>
    public class MemoryPoolRazorBufferScope : IRazorBufferScope, IDisposable
    {
        private const int SegmentSize = 1024;
        private readonly IArraySegmentPool<RazorValue> _pool;
        private List<LeasedArraySegment<RazorValue>> _leased;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MemoryPoolRazorBufferScope"/>.
        /// </summary>
        /// <param name="pool">The <see cref="IArraySegmentPool{RazorValue}"/> for creating
        /// <see cref="RazorValue"/> instances.</param>
        public MemoryPoolRazorBufferScope(IArraySegmentPool<RazorValue> pool)
        {
            _pool = pool;
        }

        /// <inheritdoc />
        public RazorBufferSegment GetSegment()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryPoolRazorBufferScope).FullName);
            }

            if (_leased == null)
            {
                _leased = new List<LeasedArraySegment<RazorValue>>(1);
            }

            LeasedArraySegment<RazorValue> segment = null;

            try
            {
                segment = _pool.Lease(SegmentSize);
                _leased.Add(segment);
            }
            catch when (segment != null)
            {
                segment.Owner.Return(segment);
                throw;
            }

            return new RazorBufferSegment(segment.Data);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_leased == null)
                {
                    return;
                }

                for (var i = 0; i < _leased.Count; i++)
                {
                    var segment = _leased[i];
                    Array.Clear(segment.Data.Array, segment.Data.Offset, segment.Data.Count);
                    segment.Owner.Return(segment);
                }

                _leased.Clear();
            }
        }
    }
}
