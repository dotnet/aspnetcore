// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Provides a multi-producer, multi-consumer thread-safe bounded segment.  When the queue is full,
    /// enqueues fail and return false.  When the queue is empty, dequeues fail and return null.
    /// These segments are linked together to form the unbounded <see cref="ConcurrentQueue{T}"/>. 
    /// </summary>
    [DebuggerDisplay("Capacity = {Capacity}")]
    internal sealed class ConcurrentQueueSegment<T>
    {
        // Segment design is inspired by the algorithm outlined at:
        // http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue

        /// <summary>The array of items in this queue.  Each slot contains the item in that slot and its "sequence number".</summary>
        internal readonly Slot[] _slots; // SOS's ThreadPool command depends on this name
        /// <summary>Mask for quickly accessing a position within the queue's array.</summary>
        internal readonly int _slotsMask;
        /// <summary>The head and tail positions, with padding to help avoid false sharing contention.</summary>
        /// <remarks>Dequeuing happens from the head, enqueuing happens at the tail.</remarks>
        internal PaddedHeadAndTail _headAndTail; // mutable struct: do not make this readonly

        /// <summary>Indicates whether the segment has been marked such that dequeues don't overwrite the removed data.</summary>
        internal bool _preservedForObservation;
        /// <summary>Indicates whether the segment has been marked such that no additional items may be enqueued.</summary>
        internal bool _frozenForEnqueues;
#pragma warning disable 0649 // some builds don't assign to this field
        /// <summary>The segment following this one in the queue, or null if this segment is the last in the queue.</summary>
        internal ConcurrentQueueSegment<T>? _nextSegment; // SOS's ThreadPool command depends on this name
#pragma warning restore 0649

        /// <summary>Creates the segment.</summary>
        /// <param name="boundedLength">
        /// The maximum number of elements the segment can contain.  Must be a power of 2.
        /// </param>
        internal ConcurrentQueueSegment(int boundedLength)
        {
            // Validate the length
            Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
            Debug.Assert((boundedLength & (boundedLength - 1)) == 0, $"Must be a power of 2, got {boundedLength}");

            // Initialize the slots and the mask.  The mask is used as a way of quickly doing "% _slots.Length",
            // instead letting us do "& _slotsMask".
            _slots = new Slot[boundedLength];
            _slotsMask = boundedLength - 1;

            // Initialize the sequence number for each slot.  The sequence number provides a ticket that
            // allows dequeuers to know whether they can dequeue and enqueuers to know whether they can
            // enqueue.  An enqueuer at position N can enqueue when the sequence number is N, and a dequeuer
            // for position N can dequeue when the sequence number is N + 1.  When an enqueuer is done writing
            // at position N, it sets the sequence number to N + 1 so that a dequeuer will be able to dequeue,
            // and when a dequeuer is done dequeueing at position N, it sets the sequence number to N + _slots.Length,
            // so that when an enqueuer loops around the slots, it'll find that the sequence number at
            // position N is N.  This also means that when an enqueuer finds that at position N the sequence
            // number is < N, there is still a value in that slot, i.e. the segment is full, and when a
            // dequeuer finds that the value in a slot is < N + 1, there is nothing currently available to
            // dequeue. (It is possible for multiple enqueuers to enqueue concurrently, writing into
            // subsequent slots, and to have the first enqueuer take longer, so that the slots for 1, 2, 3, etc.
            // may have values, but the 0th slot may still be being filled... in that case, TryDequeue will
            // return false.)
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SequenceNumber = i;
            }
        }

        /// <summary>Round the specified value up to the next power of 2, if it isn't one already.</summary>
        internal static int RoundUpToPowerOf2(int i)
        {
            // Based on https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            --i;
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        internal int FreezeOffset => _slots.Length * 2;

        /// <summary>
        /// Ensures that the segment will not accept any subsequent enqueues that aren't already underway.
        /// </summary>
        /// <remarks>
        /// When we mark a segment as being frozen for additional enqueues,
        /// we set the <see cref="_frozenForEnqueues"/> bool, but that's mostly
        /// as a small helper to avoid marking it twice.  The real marking comes
        /// by modifying the Tail for the segment, increasing it by this
        /// <see cref="FreezeOffset"/>.  This effectively knocks it off the
        /// sequence expected by future enqueuers, such that any additional enqueuer
        /// will be unable to enqueue due to it not lining up with the expected
        /// sequence numbers.  This value is chosen specially so that Tail will grow
        /// to a value that maps to the same slot but that won't be confused with
        /// any other enqueue/dequeue sequence number.
        /// </remarks>
        internal void EnsureFrozenForEnqueues() // must only be called while queue's segment lock is held
        {
            if (!_frozenForEnqueues) // flag used to ensure we don't increase the Tail more than once if frozen more than once
            {
                _frozenForEnqueues = true;

                // Increase the tail by FreezeOffset, spinning until we're successful in doing so.
                int tail = _headAndTail.Tail;
                while (true)
                {
                    int oldTail = Interlocked.CompareExchange(ref _headAndTail.Tail, tail + FreezeOffset, tail);
                    if (oldTail == tail)
                    {
                        break;
                    }
                    tail = oldTail;
                }
            }
        }

        /// <summary>Tries to dequeue an element from the queue.</summary>
        public bool TryDequeue(out T item) // TODO-NULLABLE-GENERIC
        {
            Slot[] slots = _slots;
            
            // Loop in case of contention...
            var spinner = new SpinWait();
            while (true)
            {
                // Get the head at which to try to dequeue.
                int currentHead = Volatile.Read(ref _headAndTail.Head);
                int slotsIndex = currentHead & _slotsMask;

                // Read the sequence number for the head position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

                // We can dequeue from this slot if it's been filled by an enqueuer, which
                // would have left the sequence number at pos+1.
                int diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    // We may be racing with other dequeuers.  Try to reserve the slot by incrementing
                    // the head.  Once we've done that, no one else will be able to read from this slot,
                    // and no enqueuer will be able to read from this slot until we've written the new
                    // sequence number. WARNING: The next few lines are not reliable on a runtime that
                    // supports thread aborts. If a thread abort were to sneak in after the CompareExchange
                    // but before the Volatile.Write, enqueuers trying to enqueue into this slot would
                    // spin indefinitely.  If this implementation is ever used on such a platform, this
                    // if block should be wrapped in a finally / prepared region.
                    if (Interlocked.CompareExchange(ref _headAndTail.Head, currentHead + 1, currentHead) == currentHead)
                    {
                        // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                        // trying to dequeue from this slot will end up spinning until we do the subsequent Write.
                        item = slots[slotsIndex].Item;
                        if (!Volatile.Read(ref _preservedForObservation))
                        {
                            // If we're preserving, though, we don't zero out the slot, as we need it for
                            // enumerations, peeking, ToArray, etc.  And we don't update the sequence number,
                            // so that an enqueuer will see it as full and be forced to move to a new segment.
                            slots[slotsIndex].Item = default!; // TODO-NULLABLE-GENERIC
                            Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);
                        }
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot doesn't
                    // yet contain a value we can dequeue, i.e. the segment is empty.  Technically it's
                    // possible that multiple enqueuers could have written concurrently, with those
                    // getting later slots actually finishing first, so there could be elements after
                    // this one that are available, but we need to dequeue in order.  So before declaring
                    // failure and that the segment is empty, we check the tail to see if we're actually
                    // empty or if we're just waiting for items in flight or after this one to become available.
                    bool frozen = _frozenForEnqueues;
                    int currentTail = Volatile.Read(ref _headAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                    {
                        item = default!; // TODO-NULLABLE-GENERIC
                        return false;
                    }

                    // It's possible it could have become frozen after we checked _frozenForEnqueues
                    // and before reading the tail.  That's ok: in that rare race condition, we just
                    // loop around again.
                }

                // Lost a race. Spin a bit, then try again.
                spinner.SpinOnce(sleep1Threshold: -1);
            }
        }

        /// <summary>Tries to peek at an element from the queue, without removing it.</summary>
        public bool TryPeek(out T result, bool resultUsed) // TODO-NULLABLE-GENERIC
        {
            if (resultUsed)
            {
                // In order to ensure we don't get a torn read on the value, we mark the segment
                // as preserving for observation.  Additional items can still be enqueued to this
                // segment, but no space will be freed during dequeues, such that the segment will
                // no longer be reusable.
                _preservedForObservation = true;
                Interlocked.MemoryBarrier();
            }

            Slot[] slots = _slots;

            // Loop in case of contention...
            var spinner = new SpinWait();
            while (true)
            {
                // Get the head at which to try to peek.
                int currentHead = Volatile.Read(ref _headAndTail.Head);
                int slotsIndex = currentHead & _slotsMask;

                // Read the sequence number for the head position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

                // We can peek from this slot if it's been filled by an enqueuer, which
                // would have left the sequence number at pos+1.
                int diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    result = resultUsed ? slots[slotsIndex].Item : default!; // TODO-NULLABLE-GENERIC
                    return true;
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot doesn't
                    // yet contain a value we can peek, i.e. the segment is empty.  Technically it's
                    // possible that multiple enqueuers could have written concurrently, with those
                    // getting later slots actually finishing first, so there could be elements after
                    // this one that are available, but we need to peek in order.  So before declaring
                    // failure and that the segment is empty, we check the tail to see if we're actually
                    // empty or if we're just waiting for items in flight or after this one to become available.
                    bool frozen = _frozenForEnqueues;
                    int currentTail = Volatile.Read(ref _headAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                    {
                        result = default!; // TODO-NULLABLE-GENERIC
                        return false;
                    }

                    // It's possible it could have become frozen after we checked _frozenForEnqueues
                    // and before reading the tail.  That's ok: in that rare race condition, we just
                    // loop around again.
                }

                // Lost a race. Spin a bit, then try again.
                spinner.SpinOnce(sleep1Threshold: -1);
            }
        }

        /// <summary>
        /// Attempts to enqueue the item.  If successful, the item will be stored
        /// in the queue and true will be returned; otherwise, the item won't be stored, and false
        /// will be returned.
        /// </summary>
        public bool TryEnqueue(T item)
        {
            Slot[] slots = _slots;

            // Loop in case of contention...
            var spinner = new SpinWait();
            while (true)
            {
                // Get the tail at which to try to return.
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                int slotsIndex = currentTail & _slotsMask;

                // Read the sequence number for the tail position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

                // The slot is empty and ready for us to enqueue into it if its sequence
                // number matches the slot.
                int diff = sequenceNumber - currentTail;
                if (diff == 0)
                {
                    // We may be racing with other enqueuers.  Try to reserve the slot by incrementing
                    // the tail.  Once we've done that, no one else will be able to write to this slot,
                    // and no dequeuer will be able to read from this slot until we've written the new
                    // sequence number. WARNING: The next few lines are not reliable on a runtime that
                    // supports thread aborts. If a thread abort were to sneak in after the CompareExchange
                    // but before the Volatile.Write, other threads will spin trying to access this slot.
                    // If this implementation is ever used on such a platform, this if block should be
                    // wrapped in a finally / prepared region.
                    if (Interlocked.CompareExchange(ref _headAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                    {
                        // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                        // trying to return will end up spinning until we do the subsequent Write.
                        slots[slotsIndex].Item = item;
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot still
                    // contains a value, i.e. the segment is full.  Technically it's possible that multiple
                    // dequeuers could have read concurrently, with those getting later slots actually
                    // finishing first, so there could be spaces after this one that are available, but
                    // we need to enqueue in order.
                    return false;
                }

                // Lost a race. Spin a bit, then try again.
                spinner.SpinOnce(sleep1Threshold: -1);
            }
        }

        /// <summary>Represents a slot in the queue.</summary>
        [StructLayout(LayoutKind.Auto)]
        [DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
        internal struct Slot
        {
            /// <summary>The item.</summary>
            public T Item; // SOS's ThreadPool command depends on this being at the beginning of the struct when T is a reference type
            /// <summary>The sequence number for this slot, used to synchronize between enqueuers and dequeuers.</summary>
            public int SequenceNumber;
        }
    }

    /// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
    [DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
    [StructLayout(LayoutKind.Explicit, Size = 3 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] // padding before/between/after fields
    internal struct PaddedHeadAndTail
    {
        [FieldOffset(1 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] public int Head;
        [FieldOffset(2 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] public int Tail;
    }
}
