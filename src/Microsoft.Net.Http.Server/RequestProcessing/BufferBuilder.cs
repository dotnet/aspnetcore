// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Net.Http.Server
{
    internal class BufferBuilder
    {
        private List<ArraySegment<byte>> _buffers = new List<ArraySegment<byte>>();

        internal IEnumerable<ArraySegment<byte>> Buffers
        {
            get { return _buffers; }
        }

        internal int BufferCount
        {
            get { return _buffers.Count; }
        }

        internal int TotalBytes { get; private set; }

        internal void Add(ArraySegment<byte> data)
        {
            _buffers.Add(data);
            TotalBytes += data.Count;
        }

        public void CopyAndAdd(ArraySegment<byte> data)
        {
            if (data.Count > 0)
            {
                var temp = new byte[data.Count];
                Buffer.BlockCopy(data.Array, data.Offset, temp, 0, data.Count);
                _buffers.Add(new ArraySegment<byte>(temp));
                TotalBytes += data.Count;
            }
        }

        public void Clear()
        {
            _buffers.Clear();
            TotalBytes = 0;
        }
    }
}
