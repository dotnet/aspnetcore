// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Shared.Tests.ServerInfrastructure;

public class BufferWriterTests
{
    [Fact]
    public void WriteAfterCommitAcquiresNewBuffer()
    {
        var output = new StrictBufferWriter();
        var writer = new BufferWriter<StrictBufferWriter>(output);

        writer.Write(new byte[] { 1, 2, 3 });
        writer.Commit();
        writer.Write(new byte[] { 4, 5 });
        writer.Commit();

        Assert.Equal(2, output.BufferAcquisitions);
        Assert.Collection(
            output.CommittedBuffers,
            buffer => Assert.Equal(new byte[] { 1, 2, 3 }, buffer),
            buffer => Assert.Equal(new byte[] { 4, 5 }, buffer));
    }

    private sealed class StrictBufferWriter : IBufferWriter<byte>
    {
        private byte[]? _currentLease;

        public int BufferAcquisitions { get; private set; }

        public List<byte[]> CommittedBuffers { get; } = [];

        public void Advance(int count)
        {
            if (_currentLease is null)
            {
                throw new InvalidOperationException("A new buffer must be acquired before advancing.");
            }

            if ((uint)count > (uint)_currentLease.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            CommittedBuffers.Add(_currentLease[..count]);
            _currentLease = null;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => AcquireBuffer(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => AcquireBuffer(sizeHint);

        private byte[] AcquireBuffer(int sizeHint)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            _currentLease = new byte[Math.Max(sizeHint, 16)];
            BufferAcquisitions++;
            return _currentLease;
        }
    }
}
