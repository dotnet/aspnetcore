// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.Sequences;

namespace System
{
    internal static class ByteArrayExtensions
    {
        public static ReadOnlyBytes ToChunkedReadOnlyBytes(this byte[] data, int chunkSize)
        {
            var chunks = new List<byte[]>();
            for (var i = 0; i < data.Length; i += chunkSize)
            {
                var thisChunkSize = Math.Min(chunkSize, data.Length - i);
                var chunk = new byte[thisChunkSize];
                for (var j = 0; j < thisChunkSize; j++)
                {
                    chunk[j] = data[i + j];
                }
                chunks.Add(chunk);
            }

            chunks.Reverse();

            ReadOnlyBytes? bytes = null;
            foreach (var chunk in chunks)
            {
                if (bytes == null)
                {
                    bytes = new ReadOnlyBytes(chunk);
                }
                else
                {
                    bytes = new ReadOnlyBytes(chunk, bytes);
                }
            }
            return bytes.Value;
        }
    }
}
