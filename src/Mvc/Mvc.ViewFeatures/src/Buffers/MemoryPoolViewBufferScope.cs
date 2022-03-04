// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    /// <summary>
    /// A <see cref="IViewBufferScope"/> that uses pooled memory.
    /// </summary>
    internal class MemoryPoolViewBufferScope : IViewBufferScope, IDisposable
    {
        public static readonly int MinimumSize = 16;
        private readonly ArrayPool<ViewBufferValue> _viewBufferPool;
        private readonly ArrayPool<char> _charPool;
        private List<ViewBufferValue[]> _available;
        private List<ViewBufferValue[]> _leased;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MemoryPoolViewBufferScope"/>.
        /// </summary>
        /// <param name="viewBufferPool">
        /// The <see cref="ArrayPool{ViewBufferValue}"/> for creating <see cref="ViewBufferValue"/> instances.
        /// </param>
        /// <param name="charPool">
        /// The <see cref="ArrayPool{Char}"/> for creating <see cref="PagedBufferedTextWriter"/> instances.
        /// </param>
        public MemoryPoolViewBufferScope(ArrayPool<ViewBufferValue> viewBufferPool, ArrayPool<char> charPool)
        {
            _viewBufferPool = viewBufferPool;
            _charPool = charPool;
        }

        /// <inheritdoc />
        public ViewBufferValue[] GetPage(int pageSize)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryPoolViewBufferScope).FullName);
            }

            if (_leased == null)
            {
                _leased = new List<ViewBufferValue[]>(1);
            }

            ViewBufferValue[] segment = null;

            // Reuse pages that have been returned before going back to the memory pool.
            if (_available != null && _available.Count > 0)
            {
                segment = _available[_available.Count - 1];
                _available.RemoveAt(_available.Count - 1);
                return segment;
            }

            try
            {
                segment = _viewBufferPool.Rent(Math.Max(pageSize, MinimumSize));
                _leased.Add(segment);
            }
            catch when (segment != null)
            {
                _viewBufferPool.Return(segment);
                throw;
            }

            return segment;
        }

        /// <inheritdoc />
        public void ReturnSegment(ViewBufferValue[] segment)
        {
            if (segment == null)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            Array.Clear(segment, 0, segment.Length);

            if (_available == null)
            {
                _available = new List<ViewBufferValue[]>();
            }

            _available.Add(segment);
        }

        /// <inheritdoc />
        public TextWriter CreateWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            return new PagedBufferedTextWriter(_charPool, writer);
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
                    _viewBufferPool.Return(_leased[i], clearArray: true);
                }

                _leased.Clear();
            }
        }
    }
}
