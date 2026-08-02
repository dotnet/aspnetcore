// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/roslyn-analyzers
// https://github.com/dotnet/roslyn-analyzers/blob/1dc431ec828e9cc816fc69f89ad9b8286e74d707/src/Utilities/Compiler/RoslynHashCode.cs

/*

The xxHash32 implementation is based on the code published by Yann Collet:
https://raw.githubusercontent.com/Cyan4973/xxHash/5c174cfa4e45a42f94082dc0d4539b39696afea1/xxhash.c

  xxHash - Fast Hash algorithm
  Copyright (C) 2012-2016, Yann Collet

  BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions are
  met:

  * Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.
  * Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following disclaimer
  in the documentation and/or other materials provided with the
  distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

  You can contact the author at :
  - xxHash homepage: http://www.xxhash.com
  - xxHash source repository : https://github.com/Cyan4973/xxHash

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

[SuppressMessage("Design", "CA1066:Implement IEquatable when overriding Object.Equals", Justification = "This type is not equatable.")]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This type is not equatable.")]
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "This type is not equatable.")]
internal struct HashCode
{
    private static readonly uint s_seed = GenerateGlobalSeed();

    private const uint Prime1 = 2654435761U;
    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    private uint _v1, _v2, _v3, _v4;
    private uint _queue1, _queue2, _queue3;
    private uint _length;

    private static uint GenerateGlobalSeed()
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        var array = new byte[sizeof(uint)];
        randomNumberGenerator.GetBytes(array);
        return BitConverter.ToUInt32(array, 0);
    }

    public static int Combine<T1>(T1 value1)
    {
        // Provide a way of diffusing bits from something with a limited
        // input hash space. For example, many enums only have a few
        // possible hashes, only using the bottom few bits of the code. Some
        // collections are built on the assumption that hashes are spread
        // over a larger space, so diffusing the bits may help the
        // collection work more efficiently.

        var hc1 = (uint)(value1?.GetHashCode() ?? 0);

        var hash = MixEmptyState();
        hash += 4;

        hash = QueueRound(hash, hc1);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);

        var hash = MixEmptyState();
        hash += 8;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);

        var hash = MixEmptyState();
        hash += 12;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = QueueRound(hash, hc3);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);
        var hc4 = (uint)(value4?.GetHashCode() ?? 0);

        Initialize(out var v1, out var v2, out var v3, out var v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        var hash = MixState(v1, v2, v3, v4);
        hash += 16;

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);
        var hc4 = (uint)(value4?.GetHashCode() ?? 0);
        var hc5 = (uint)(value5?.GetHashCode() ?? 0);

        Initialize(out var v1, out var v2, out var v3, out var v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        var hash = MixState(v1, v2, v3, v4);
        hash += 20;

        hash = QueueRound(hash, hc5);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);
        var hc4 = (uint)(value4?.GetHashCode() ?? 0);
        var hc5 = (uint)(value5?.GetHashCode() ?? 0);
        var hc6 = (uint)(value6?.GetHashCode() ?? 0);

        Initialize(out var v1, out var v2, out var v3, out var v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        var hash = MixState(v1, v2, v3, v4);
        hash += 24;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);
        var hc4 = (uint)(value4?.GetHashCode() ?? 0);
        var hc5 = (uint)(value5?.GetHashCode() ?? 0);
        var hc6 = (uint)(value6?.GetHashCode() ?? 0);
        var hc7 = (uint)(value7?.GetHashCode() ?? 0);

        Initialize(out var v1, out var v2, out var v3, out var v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        var hash = MixState(v1, v2, v3, v4);
        hash += 28;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = QueueRound(hash, hc7);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);
        var hc3 = (uint)(value3?.GetHashCode() ?? 0);
        var hc4 = (uint)(value4?.GetHashCode() ?? 0);
        var hc5 = (uint)(value5?.GetHashCode() ?? 0);
        var hc6 = (uint)(value6?.GetHashCode() ?? 0);
        var hc7 = (uint)(value7?.GetHashCode() ?? 0);
        var hc8 = (uint)(value8?.GetHashCode() ?? 0);

        Initialize(out var v1, out var v2, out var v3, out var v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        v1 = Round(v1, hc5);
        v2 = Round(v2, hc6);
        v3 = Round(v3, hc7);
        v4 = Round(v4, hc8);

        var hash = MixState(v1, v2, v3, v4);
        hash += 32;

        hash = MixFinal(hash);
        return (int)hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = s_seed + Prime1 + Prime2;
        v2 = s_seed + Prime2;
        v3 = s_seed;
        v4 = s_seed - Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round(uint hash, uint input)
    {
        return BitOperations.RotateLeft(hash + (input * Prime2), 13) * Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QueueRound(uint hash, uint queuedValue)
    {
        return BitOperations.RotateLeft(hash + (queuedValue * Prime3), 17) * Prime4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixState(uint v1, uint v2, uint v3, uint v4)
    {
        return BitOperations.RotateLeft(v1, 1) + BitOperations.RotateLeft(v2, 7) + BitOperations.RotateLeft(v3, 12) + BitOperations.RotateLeft(v4, 18);
    }

    private static uint MixEmptyState()
    {
        return s_seed + Prime5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    public void Add<T>(T value)
    {
        Add(value?.GetHashCode() ?? 0);
    }

    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        Add(value is null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode()));
    }

    private void Add(int value)
    {
        // The original xxHash works as follows:
        // 0. Initialize immediately. We can't do this in a struct (no
        //    default ctor).
        // 1. Accumulate blocks of length 16 (4 uints) into 4 accumulators.
        // 2. Accumulate remaining blocks of length 4 (1 uint) into the
        //    hash.
        // 3. Accumulate remaining blocks of length 1 into the hash.

        // There is no need for #3 as this type only accepts ints. _queue1,
        // _queue2 and _queue3 are basically a buffer so that when
        // ToHashCode is called we can execute #2 correctly.

        // We need to initialize the xxHash32 state (_v1 to _v4) lazily (see
        // #0) nd the last place that can be done if you look at the
        // original code is just before the first block of 16 bytes is mixed
        // in. The xxHash32 state is never used for streams containing fewer
        // than 16 bytes.

        // To see what's really going on here, have a look at the Combine
        // methods.

        var val = (uint)value;

        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        var previousLength = _length++;
        var position = previousLength % 4;

        // Switch can't be inlined.

        if (position == 0)
        {
            _queue1 = val;
        }
        else if (position == 1)
        {
            _queue2 = val;
        }
        else if (position == 2)
        {
            _queue3 = val;
        }
        else // position == 3
        {
            if (previousLength == 3)
            {
                Initialize(out _v1, out _v2, out _v3, out _v4);
            }

            _v1 = Round(_v1, _queue1);
            _v2 = Round(_v2, _queue2);
            _v3 = Round(_v3, _queue3);
            _v4 = Round(_v4, val);
        }
    }

    public int ToHashCode()
    {
        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        var length = _length;

        // position refers to the *next* queue position in this method, so
        // position == 1 means that _queue1 is populated; _queue2 would have
        // been populated on the next call to Add.
        var position = length % 4;

        // If the length is less than 4, _v1 to _v4 don't contain anything
        // yet. xxHash32 treats this differently.

        var hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

        // _length is incremented once per Add(Int32) and is therefore 4
        // times too small (xxHash length is in bytes, not ints).

        hash += length * 4;

        // Mix what remains in the queue

        // Switch can't be inlined right now, so use as few branches as
        // possible by manually excluding impossible scenarios (position > 1
        // is always false if position is not > 0).
        if (position > 0)
        {
            hash = QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, _queue2);
                if (position > 2)
                {
                    hash = QueueRound(hash, _queue3);
                }
            }
        }

        hash = MixFinal(hash);
        return (int)hash;
    }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
    // Obsolete member 'memberA' overrides non-obsolete member 'memberB'.
    // Disallowing GetHashCode and Equals is by design

    // * We decided to not override GetHashCode() to produce the hash code
    //   as this would be weird, both naming-wise as well as from a
    //   behavioral standpoint (GetHashCode() should return the object's
    //   hash code, not the one being computed).

    // * Even though ToHashCode() can be called safely multiple times on
    //   this implementation, it is not part of the contract. If the
    //   implementation has to change in the future we don't want to worry
    //   about people who might have incorrectly used this type.

    [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException(SR.HashCode_HashCodeNotSupported);

    [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException(SR.HashCode_EqualityNotSupported);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    private static class SR
    {
        public static string HashCode_HashCodeNotSupported = "HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.";
        public static string HashCode_EqualityNotSupported = "HashCode is a mutable struct and should not be compared with other HashCodes.";
    }

    private static class BitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateLeft(ulong value, int offset)
            => (value << offset) | (value >> (64 - offset));
    }
}
