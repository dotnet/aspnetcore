using System;
using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public sealed class MemoryBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private readonly int _segmentSize;
        private int _bytesWritten;

        internal List<byte[]> Segments { get; }
        internal int Position { get; private set; }

        public MemoryBufferWriter(int segmentSize = 2048)
        {
            _segmentSize = segmentSize;

            Segments = new List<byte[]>();
        }

        public Memory<byte> CurrentSegment => Segments.Count > 0 ? Segments[Segments.Count - 1] : null;

        public void Advance(int count)
        {
            _bytesWritten += count;
            Position += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            // TODO: Use sizeHint

            if (Segments.Count == 0 || Position == _segmentSize)
            {
                Segments.Add(ArrayPool<byte>.Shared.Rent(_segmentSize));
                Position = 0;
            }

            return CurrentSegment.Slice(Position, CurrentSegment.Length - Position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        public byte[] ToArray()
        {
            if (Segments.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[_bytesWritten];

            var totalWritten = 0;

            // Copy full segments
            for (int i = 0; i < Segments.Count - 1; i++)
            {
                Segments[i].AsMemory().CopyTo(result.AsMemory(totalWritten, _segmentSize));

                totalWritten += _segmentSize;
            }

            // Copy current incomplete segment
            CurrentSegment.Slice(0, Position).CopyTo(result.AsMemory(totalWritten, Position));

            return result;
        }

        public void Dispose()
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                ArrayPool<byte>.Shared.Return(Segments[i]);
            }
            Segments.Clear();
        }
    }
}