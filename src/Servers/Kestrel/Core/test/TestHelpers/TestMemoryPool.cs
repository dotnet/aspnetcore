// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestMemoryPool : MemoryPool<byte>
{
    public override int MaxBufferSize => 4096;

    public override IMemoryOwner<byte> Rent(int minBufferSize = -1) => new TestMemoryOwner(Math.Max(minBufferSize, 4096));

    protected override void Dispose(bool disposing)
    {
    }

    private sealed class TestMemoryOwner : IMemoryOwner<byte>
    {
        private readonly int _size;

        public TestMemoryOwner(int size)
        {
            _size = size;
        }

        public Memory<byte> Memory
        {
            get
            {
                var memory = new byte[_size];
                memory.AsSpan().Fill(0xff);
                return memory;
            }
        }

        public void Dispose()
        {
        }
    }
}
