using System;
using System.Buffers;
using System.IO.Pipelines;

namespace PlatformBenchmarks
{
    public class MemoryWriter : IBufferWriter<byte>
    {
        private PipeWriter _writer;
        private Memory<byte> _memory;
        private int _buffered;

        public MemoryWriter(PipeWriter writer)
        {
            _writer = writer;
        }

        public void Advance(int count)
        {
            _buffered += count;
            _memory = _memory.Slice(count);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (_memory.Length < sizeHint)
            {
                if (_buffered > 0)
                {
                    _writer.Advance(_buffered);
                    _buffered = 0;
                }

                _memory = _writer.GetMemory(sizeHint);
            }

            return _memory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (_memory.Length >= sizeHint)
            {
                return _memory.Span;
            }

            return GetMemory(sizeHint).Span;
        }

        public void Commit()
        {
            var buffered = _buffered;
            if (buffered > 0)
            {
                _buffered = 0;
                _writer.Advance(buffered);
            }

            _memory = default;
        }

        public BufferWriter GetBufferWriter() => new BufferWriter(this);
    }
}
