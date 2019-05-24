// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    internal sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        private object _memoryOwner;
        private BufferSegment _next;
        private int _end;

        /// <summary>
        /// The End represents the offset into AvailableMemory where the range of "active" bytes ends. At the point when the block is leased
        /// the End is guaranteed to be equal to Start. The value of Start may be assigned anywhere between 0 and
        /// Buffer.Length, and must be equal to or less than End.
        /// </summary>
        public int End
        {
            get => _end;
            set
            {
                Debug.Assert(value <= AvailableMemory.Length);

                _end = value;
                Memory = AvailableMemory.Slice(0, _end);
            }
        }

        /// <summary>
        /// Reference to the next block of data when the overall "active" bytes spans multiple blocks. At the point when the block is
        /// leased Next is guaranteed to be null. Start, End, and Next are used together in order to create a linked-list of discontiguous
        /// working memory. The "active" memory is grown when bytes are copied in, End is increased, and Next is assigned. The "active"
        /// memory is shrunk when bytes are consumed, Start is increased, and blocks are returned to the pool.
        /// </summary>
        public BufferSegment NextSegment
        {
            get => _next;
            set
            {
                _next = value;
                Next = value;
            }
        }

        public void SetMemory(object memoryOwner)
        {
            if (memoryOwner is IMemoryOwner<byte> owner)
            {
                SetMemory(owner);
            }
            else if (memoryOwner is byte[] array)
            {
                SetMemory(array);
            }
            else
            {
                Debug.Fail("Unexpected memoryOwner");
            }
        }

        public void SetMemory(IMemoryOwner<byte> memoryOwner)
        {
            _memoryOwner = memoryOwner;

            SetUnownedMemory(memoryOwner.Memory);
        }

        public void SetMemory(byte[] arrayPoolBuffer)
        {
            _memoryOwner = arrayPoolBuffer;

            SetUnownedMemory(arrayPoolBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUnownedMemory(Memory<byte> memory)
        {
            AvailableMemory = memory;
            RunningIndex = 0;
            End = 0;
            NextSegment = null;
        }

        public void ResetMemory()
        {
            if (_memoryOwner is IMemoryOwner<byte> owner)
            {
                owner.Dispose();
            }
            else if (_memoryOwner is byte[] array)
            {
                ArrayPool<byte>.Shared.Return(array);
            }

            _memoryOwner = null;
            AvailableMemory = default;
        }

        // Exposed for testing
        internal object MemoryOwner => _memoryOwner;

        public Memory<byte> AvailableMemory { get; private set; }

        public int Length => End;

        /// <summary>
        /// The amount of writable bytes in this segment. It is the amount of bytes between Length and End
        /// </summary>
        public int WritableBytes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AvailableMemory.Length - End;
        }

        public void SetNext(BufferSegment segment)
        {
            Debug.Assert(segment != null);
            Debug.Assert(Next == null);

            NextSegment = segment;

            segment = this;

            while (segment.Next != null)
            {
                segment.NextSegment.RunningIndex = segment.RunningIndex + segment.Length;
                segment = segment.NextSegment;
            }
        }
    }
}
