// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.OutputCaching;

// TODO: reinstate pooling
// context: a last-minute bug was detected during net8 preparation that impacted
// buffer reuse from output-cache (the only consumer of this type); the preferred
// solution for this is understood, but is more "moving parts" than we're comfortable
// touching in the last phase of net8, so to avoid risk, *temporarily*, the buffer
// reuse is disabled; this is consistent with net7, which never used buffer recycling
// in output-cache, so this is not a regression. The work to properly implement buffer
// reuse in output-cache is in progress to be merged in net9 and hopefully backported
// into a net8 service release.
internal sealed class RecyclableReadOnlySequenceSegment : ReadOnlySequenceSegment<byte>
{
    public int Length => Memory.Length;
    private RecyclableReadOnlySequenceSegment() { }

    public static RecyclableReadOnlySequenceSegment Create(int minimumLength, RecyclableReadOnlySequenceSegment? previous)
        => Create(GetBuffer(minimumLength), previous);

    public static RecyclableReadOnlySequenceSegment Create(ReadOnlyMemory<byte> memory, RecyclableReadOnlySequenceSegment? previous)
    {
        var obj = s_Spares.TryDequeue(out var value) ? value : new();
        obj.Memory = memory;
        if (previous is not null)
        {
            obj.RunningIndex = previous.RunningIndex + previous.Length;
            previous.Next = obj;
        }
        return obj;
    }

    private const int TARGET_MAX = 128;
    static readonly ConcurrentQueue<RecyclableReadOnlySequenceSegment> s_Spares = new();

    public static void RecycleChain(RecyclableReadOnlySequenceSegment? obj, bool recycleBuffers = false)
    {
        while (obj is not null)
        {
            var mem = obj.Memory;
            obj.Memory = default;
            obj.RunningIndex = 0;
            var next = obj.Next as RecyclableReadOnlySequenceSegment;
            obj.Next = default;
            if (s_Spares.Count < TARGET_MAX) // not precise, due to not wanting lock
            { // (note: we still want to break the chain, even if not reusing; no else-break)
                s_Spares.Enqueue(obj);
            }
            if (recycleBuffers)
            {
                Recycle(mem);
            }
            obj = next;
        }
    }
    public static void RecycleChain(in ReadOnlySequence<byte> value, bool recycleBuffers = false)
    {
        var obj = value.Start.GetObject() as RecyclableReadOnlySequenceSegment;
        if (obj is null)
        {
            // not segment based, but memory may still need recycling
            if (recycleBuffers)
            {
                Recycle(value.First);
            }
        }
        else
        {
            RecycleChain(obj, recycleBuffers);
        }
    }

    internal static ReadOnlySequence<byte> CreateSequence(IList<byte[]> segments)
    {
        if (segments is null)
        {
            return default;
        }
        int count = segments.Count;
        switch (count)
        {
            case 0:
                return default;
            case 1:
                return new(segments[0]);
            default:
                RecyclableReadOnlySequenceSegment first = Create(segments[0], null), last = first;
                for (int i = 1; i < count; i++)
                {
                    last = Create(segments[i], last);
                }
                return new(first, 0, last, last.Length);
        }
    }

    public static async ValueTask CopyToAsync(ReadOnlySequence<byte> source, PipeWriter destination, CancellationToken cancellationToken)
    {
        if (!source.IsEmpty)
        {
            if (source.IsSingleSegment)
            {
                await destination.WriteAsync(source.First, cancellationToken);
            }
            else
            {
                foreach (var segment in source)
                {
                    if (!segment.IsEmpty)
                    {
                        await destination.WriteAsync(segment, cancellationToken);
                    }
                }
            }
        }
    }

    // TODO: reinstate ArrayPool<byte>.Shared usage.Rent(minimumLength);
    private static byte[] GetBuffer(int minimumLength)
        => new byte[minimumLength];

    private static void Recycle(ReadOnlyMemory<byte> _)
    {
        // TODO: reinstate buffer recycling
        //if (MemoryMarshal.TryGetArray(value, out var segment) && segment.Offset == 0 && segment.Count != 0)
        //{
        //    ArrayPool<byte>.Shared.Return(segment.Array!);
        //}
    }
}
