// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Buffer
{
    /// <summary>
    /// A <see cref="IViewBufferScope"/> that uses pooled memory.
    /// </summary>
    public class MemoryPoolViewBufferScope : IViewBufferScope, IDisposable
    {
        public static readonly int SegmentSize = 512;
        private readonly ArrayPool<ViewBufferValue> _pool;
        private List<ViewBufferValue[]> _leased;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MemoryPoolViewBufferScope"/>.
        /// </summary>
        /// <param name="pool">The <see cref="ArrayPool{ViewBufferValue}"/> for creating
        /// <see cref="ViewBufferValue"/> instances.</param>
        public MemoryPoolViewBufferScope(ArrayPool<ViewBufferValue> pool)
        {
            _pool = pool;
        }

        /// <inheritdoc />
        public ViewBufferValue[] GetSegment()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryPoolViewBufferScope).FullName);
            }

            if (_leased == null)
            {
                _leased = new List<ViewBufferValue[]>(1);
            }

            ViewBufferValue[] segment = null;

            try
            {
                segment = _pool.Rent(SegmentSize);
                _leased.Add(segment);
            }
            catch when (segment != null)
            {
                _pool.Return(segment);
                throw;
            }

            return segment;
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
                    _pool.Return(_leased[i]);
                }

                _leased.Clear();
            }
        }
    }
}
