// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.Streams;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Manages a sequence of elements, readily castable as a <see cref="ReadOnlySequence{T}"/>.
/// </summary>
/// <typeparam name="T">The type of element stored by the sequence.</typeparam>
/// <remarks>
/// Instance members are not thread-safe.
/// </remarks>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
internal sealed class Sequence<T> : IBufferWriter<T>, IDisposable
{
    private static readonly int DefaultLengthFromArrayPool = 1 + (4095 / Marshal.SizeOf<T>());

    private readonly Stack<SequenceSegment> segmentPool = new Stack<SequenceSegment>();

    private readonly MemoryPool<T> memoryPool;

    private readonly ArrayPool<T> arrayPool;

    private SequenceSegment first;

    private SequenceSegment last;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sequence{T}"/> class
    /// that uses a private <see cref="ArrayPool{T}"/> for recycling arrays.
    /// </summary>
    public Sequence()
        : this(ArrayPool<T>.Create())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sequence{T}"/> class.
    /// </summary>
    /// <param name="memoryPool">The pool to use for recycling backing arrays.</param>
    public Sequence(MemoryPool<T> memoryPool)
    {
        Requires.NotNull(memoryPool, nameof(memoryPool));
        this.memoryPool = memoryPool;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sequence{T}"/> class.
    /// </summary>
    /// <param name="arrayPool">The pool to use for recycling backing arrays.</param>
    public Sequence(ArrayPool<T> arrayPool)
    {
        Requires.NotNull(arrayPool, nameof(arrayPool));
        this.arrayPool = arrayPool;
    }

    /// <summary>
    /// Gets or sets the minimum length for any array allocated as a segment in the sequence.
    /// Any non-positive value allows the pool to determine the length of the array.
    /// </summary>
    /// <value>The default value is 0.</value>
    /// <remarks>
    /// <para>
    /// Each time <see cref="GetSpan(int)"/> or <see cref="GetMemory(int)"/> is called,
    /// previously allocated memory is used if it is large enough to satisfy the length demand.
    /// If new memory must be allocated, the argument to one of these methods typically dictate
    /// the length of array to allocate. When the caller uses very small values (just enough for its immediate need)
    /// but the high level scenario can predict that a large amount of memory will be ultimately required,
    /// it can be advisable to set this property to a value such that just a few larger arrays are allocated
    /// instead of many small ones.
    /// </para>
    /// <para>
    /// The <see cref="MemoryPool{T}"/> in use may itself have a minimum array length as well,
    /// in which case the higher of the two minimums dictate the minimum array size that will be allocated.
    /// </para>
    /// </remarks>
    public int MinimumSpanLength { get; set; }

    /// <summary>
    /// Gets this sequence expressed as a <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <returns>A read only sequence representing the data in this object.</returns>
    public ReadOnlySequence<T> AsReadOnlySequence => this;

    /// <summary>
    /// Gets the length of the sequence.
    /// </summary>
    public long Length => this.AsReadOnlySequence.Length;

    /// <summary>
    /// Gets the value to display in a debugger datatip.
    /// </summary>
    private string DebuggerDisplay => $"Length = {AsReadOnlySequence.Length}";

    /// <summary>
    /// Expresses this sequence as a <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <param name="sequence">The sequence to convert.</param>
    public static implicit operator ReadOnlySequence<T>(Sequence<T> sequence)
    {
        return sequence.first != null
            ? new ReadOnlySequence<T>(sequence.first, sequence.first.Start, sequence.last, sequence.last.End)
            : ReadOnlySequence<T>.Empty;
    }

    /// <summary>
    /// Removes all elements from the sequence from its beginning to the specified position,
    /// considering that data to have been fully processed.
    /// </summary>
    /// <param name="position">
    /// The position of the first element that has not yet been processed.
    /// This is typically <see cref="ReadOnlySequence{T}.End"/> after reading all elements from that instance.
    /// </param>
    public void AdvanceTo(SequencePosition position)
    {
        var firstSegment = (SequenceSegment)position.GetObject();
        int firstIndex = position.GetInteger();

        // Before making any mutations, confirm that the block specified belongs to this sequence.
        var current = this.first;
        while (current != firstSegment && current != null)
        {
            current = current.Next;
        }

        Requires.Argument(current != null, nameof(position), "Position does not represent a valid position in this sequence.");

        // Also confirm that the position is not a prior position in the block.
        Requires.Argument(firstIndex >= current.Start, nameof(position), "Position must not be earlier than current position.");

        // Now repeat the loop, performing the mutations.
        current = this.first;
        while (current != firstSegment)
        {
            current = this.RecycleAndGetNext(current);
        }

        firstSegment.AdvanceTo(firstIndex);

        if (firstSegment.Length == 0)
        {
            firstSegment = this.RecycleAndGetNext(firstSegment);
        }

        this.first = firstSegment;

        if (this.first == null)
        {
            this.last = null;
        }
    }

    /// <summary>
    /// Advances the sequence to include the specified number of elements initialized into memory
    /// returned by a prior call to <see cref="GetMemory(int)"/>.
    /// </summary>
    /// <param name="count">The number of elements written into memory.</param>
    public void Advance(int count)
    {
        SequenceSegment last = this.last;
        Debug.Assert(last != null, "Cannot advance before acquiring memory.");
        last.Advance(count);
    }

    /// <summary>
    /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
    /// </summary>
    /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
    /// <returns>The requested memory.</returns>
    public Memory<T> GetMemory(int sizeHint) => this.GetSegment(sizeHint).RemainingMemory;

    /// <summary>
    /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
    /// </summary>
    /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
    /// <returns>The requested memory.</returns>
    public Span<T> GetSpan(int sizeHint) => this.GetSegment(sizeHint).RemainingSpan;

    /// <summary>
    /// Clears the entire sequence, recycles associated memory into pools,
    /// and resets this instance for reuse.
    /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Dispose() => this.Reset();

    /// <summary>
    /// Clears the entire sequence and recycles associated memory into pools.
    /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
    /// </summary>
    public void Reset()
    {
        var current = this.first;
        while (current != null)
        {
            current = this.RecycleAndGetNext(current);
        }

        this.first = this.last = null;
    }

    private SequenceSegment GetSegment(int sizeHint)
    {
        Requires.Range(sizeHint >= 0, nameof(sizeHint));
        int? minBufferSize = null;
        if (sizeHint == 0)
        {
            if (this.last == null || this.last.WritableBytes == 0)
            {
                // We're going to need more memory. Take whatever size the pool wants to give us.
                minBufferSize = -1;
            }
        }
        else
        {
            sizeHint = Math.Max(this.MinimumSpanLength, sizeHint);
            if (this.last == null || this.last.WritableBytes < sizeHint)
            {
                minBufferSize = sizeHint;
            }
        }

        if (minBufferSize.HasValue)
        {
            var segment = this.segmentPool.Count > 0 ? this.segmentPool.Pop() : new SequenceSegment();
            if (this.arrayPool != null)
            {
                segment.Assign(this.arrayPool.Rent(minBufferSize.Value == -1 ? DefaultLengthFromArrayPool : minBufferSize.Value));
            }
            else
            {
                segment.Assign(this.memoryPool.Rent(minBufferSize.Value));
            }

            this.Append(segment);
        }

        return this.last;
    }

    private void Append(SequenceSegment segment)
    {
        if (this.last == null)
        {
            this.first = this.last = segment;
        }
        else
        {
            if (this.last.Length > 0)
            {
                // Add a new block.
                this.last.SetNext(segment);
            }
            else
            {
                // The last block is completely unused. Replace it instead of appending to it.
                var current = this.first;
                if (this.first != this.last)
                {
                    while (current.Next != this.last)
                    {
                        current = current.Next;
                    }
                }
                else
                {
                    this.first = segment;
                }

                current.SetNext(segment);
                this.RecycleAndGetNext(this.last);
            }

            this.last = segment;
        }
    }

    private SequenceSegment RecycleAndGetNext(SequenceSegment segment)
    {
        var recycledSegment = segment;
        segment = segment.Next;
        recycledSegment.ResetMemory(this.arrayPool);
        this.segmentPool.Push(recycledSegment);
        return segment;
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<T>
    {
        /// <summary>
        /// A value indicating whether the element is a value type.
        /// </summary>
        private static readonly bool IsValueTypeElement = typeof(T).IsValueType;

        /// <summary>
        /// Gets the backing array, when using an <see cref="ArrayPool{T}"/> instead of a <see cref="MemoryPool{T}"/>.
        /// </summary>
        private T[] array;

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data starts.
        /// </summary>
        /// <remarks>This may be nonzero as a result of calling <see cref="Sequence{T}.AdvanceTo(SequencePosition)"/>.</remarks>
        internal int Start { get; private set; }

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data ends.
        /// </summary>
        internal int End { get; private set; }

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Memory<T> RemainingMemory => this.AvailableMemory.Slice(this.End);

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Span<T> RemainingSpan => this.AvailableMemory.Span.Slice(this.End);

        /// <summary>
        /// Gets the tracker for the underlying array for this segment, which can be used to recycle the array when we're disposed of.
        /// Will be <c>null</c> if using an array pool, in which case the memory is held by <see cref="array"/>.
        /// </summary>
        internal IMemoryOwner<T> MemoryOwner { get; private set; }

        /// <summary>
        /// Gets the full memory owned by the <see cref="MemoryOwner"/>.
        /// </summary>
        internal Memory<T> AvailableMemory => this.array ?? this.MemoryOwner?.Memory ?? default;

        /// <summary>
        /// Gets the number of elements that are committed in this segment.
        /// </summary>
        internal int Length => this.End - this.Start;

        /// <summary>
        /// Gets the amount of writable bytes in this segment.
        /// It is the amount of bytes between <see cref="Length"/> and <see cref="End"/>.
        /// </summary>
        internal int WritableBytes => this.AvailableMemory.Length - this.End;

        /// <summary>
        /// Gets or sets the next segment in the singly linked list of segments.
        /// </summary>
        internal new SequenceSegment Next
        {
            get => (SequenceSegment)base.Next;
            set => base.Next = value;
        }

        /// <summary>
        /// Assigns this (recyclable) segment a new area in memory.
        /// </summary>
        /// <param name="memoryOwner">The memory and a means to recycle it.</param>
        internal void Assign(IMemoryOwner<T> memoryOwner)
        {
            this.MemoryOwner = memoryOwner;
            this.Memory = memoryOwner.Memory;
        }

        /// <summary>
        /// Assigns this (recyclable) segment a new area in memory.
        /// </summary>
        /// <param name="array">An array drawn from an <see cref="ArrayPool{T}"/>.</param>
        internal void Assign(T[] array)
        {
            this.array = array;
            this.Memory = array;
        }

        /// <summary>
        /// Clears all fields in preparation to recycle this instance.
        /// </summary>
        internal void ResetMemory(ArrayPool<T> arrayPool)
        {
            this.ClearReferences(this.Start, this.End);
            this.Memory = default;
            this.Next = null;
            this.RunningIndex = 0;
            this.Start = 0;
            this.End = 0;
            if (this.array != null)
            {
                arrayPool.Return(this.array);
                this.array = null;
            }
            else
            {
                this.MemoryOwner?.Dispose();
                this.MemoryOwner = null;
            }
        }

        /// <summary>
        /// Adds a new segment after this one.
        /// </summary>
        /// <param name="segment">The next segment in the linked list.</param>
        internal void SetNext(SequenceSegment segment)
        {
            Debug.Assert(segment != null, "Null not allowed.");
            this.Next = segment;
            segment.RunningIndex = this.RunningIndex + this.Start + this.Length;

            // When setting Memory, we start with index 0 instead of this.Start because
            // the first segment has an explicit index set anyway,
            // and we don't want to double-count it here.
            this.Memory = this.AvailableMemory.Slice(0, this.Start + this.Length);
        }

        /// <summary>
        /// Commits more elements as written in this segment.
        /// </summary>
        /// <param name="count">The number of elements written.</param>
        internal void Advance(int count)
        {
            Requires.Range(count >= 0 && this.End + count <= this.Memory.Length, nameof(count));
            this.End += count;
        }

        /// <summary>
        /// Removes some elements from the start of this segment.
        /// </summary>
        /// <param name="offset">The number of elements to ignore from the start of the underlying array.</param>
        internal void AdvanceTo(int offset)
        {
            Debug.Assert(offset >= this.Start, "Trying to rewind.");
            this.ClearReferences(this.Start, offset - this.Start);
            this.Start = offset;
        }

        private void ClearReferences(int startIndex, int length)
        {
            // If we store references, clear them to allow the objects to be GC'd.
            if (!IsValueTypeElement)
            {
                this.AvailableMemory.Span.Slice(startIndex, length).Clear();
            }
        }
    }
}
