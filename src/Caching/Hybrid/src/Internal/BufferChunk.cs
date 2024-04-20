// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

// used to convey buffer status; like ArraySegment<byte>, but Offset is always
// zero, and we use the most significant bit (MSB, the sign flag) of the length
// to track whether or not to recycle this value
internal readonly struct BufferChunk
{
    private const int MSB = (1 << 31);

    private readonly int _lengthAndPoolFlag;
    public byte[]? Array { get; } // null for default

    public int Length => _lengthAndPoolFlag & ~MSB;

    public bool ReturnToPool => (_lengthAndPoolFlag & MSB) != 0;

    public byte[] ToArray()
    {
        var length = Length;
        if (length == 0)
        {
            return [];
        }

        var copy = new byte[length];
        Buffer.BlockCopy(Array!, 0, copy, 0, length);
        return copy;
    }

    public BufferChunk(byte[] array)
    {
        Debug.Assert(array is not null, "expected valid array input");
        Array = array;
        _lengthAndPoolFlag = array.Length;
        // assume not pooled, if exact-sized
        // (we don't expect array.Length to be negative; we're really just saying
        // "we expect the result of assigning array.Length to _lengthAndPoolFlag
        // to give the expected Length *and* not have the MSB set; we're just
        // checking that we haven't fat-fingered our MSB logic)
        Debug.Assert(!ReturnToPool, "do not return right-sized arrays");
        Debug.Assert(Length == array.Length, "array length not respected");
    }

    public BufferChunk(byte[] array, int length, bool returnToPool)
    {
        Debug.Assert(array is not null, "expected valid array input");
        Debug.Assert(length >= 0, "expected valid length");
        Array = array;
        _lengthAndPoolFlag = length | (returnToPool ? MSB : 0);
        Debug.Assert(ReturnToPool == returnToPool, "return-to-pool not respected");
        Debug.Assert(Length == length, "length not respected");
    }

    internal void RecycleIfAppropriate()
    {
        if (ReturnToPool)
        {
            ArrayPool<byte>.Shared.Return(Array!);
        }
        Unsafe.AsRef(in this) = default; // anti foot-shotgun double-return guard; not 100%, but worth doing
        Debug.Assert(Array is null && !ReturnToPool, "expected clean slate after recycle");
    }

    internal ReadOnlySequence<byte> AsSequence() => Length == 0 ? default : new ReadOnlySequence<byte>(Array!, 0, Length);

    internal BufferChunk DoNotReturnToPool()
    {
        var copy = this;
        Unsafe.AsRef(in copy._lengthAndPoolFlag) &= ~MSB;
        Debug.Assert(copy.Length == Length, "same length expected");
        Debug.Assert(!copy.ReturnToPool, "do not return to pool");
        return copy;
    }
}
