// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal ref struct BufferWriter<T> where T: IBufferWriter<byte>
    {
        private T _output;
        private Span<byte> _span;
        private int _buffered;

        public BufferWriter(T output)
        {
            _buffered = 0;
            _output = output;
            _span = output.GetSpan();
        }

        public Span<byte> Span => _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            var buffered = _buffered;
            if (buffered > 0)
            {
                _buffered = 0;
                _output.Advance(buffered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _buffered += count;
            _span = _span.Slice(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> source)
        {
            if (_span.Length >= source.Length)
            {
                source.CopyTo(_span);
                Advance(source.Length);
            }
            else
            {
                WriteMultiBuffer(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (_span.Length < count)
            {
                EnsureMore(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            if (_buffered > 0)
            {
                Commit();
            }

            _output.GetMemory(count);
            _span = _output.GetSpan();
        }

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                var writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
