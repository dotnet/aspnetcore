// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel;

/// <summary>
/// A pooled HTTP/2 or HTTP/3 stream.
/// </summary>
internal interface IPooledStream
{
    long PoolExpirationTimestamp { get; }
    void DisposeCore();
}

/// <summary>
/// A pool of <see cref="IPooledStream"/> instances.
/// </summary>
/// <typeparam name="TValue">The type of stream.</typeparam>
/// <remarks>
/// Inspired by https://github.com/dotnet/runtime/blob/da9b16f2804e87c9c1ca9dcd9036e7b53e724f5d/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/BufferSegmentStack.cs
/// <para/>
/// We seem to have chosen a stack for its quick insertion and removal, rather than for LIFO semantics.
/// <para/>
/// Owned by an Http2Connection or QuicConnectionContext.
/// </remarks>
internal struct PooledStreamStack<TValue> where TValue : class, IPooledStream
{
    // Internal for testing
    internal StreamAsValueType[] _array;
    private int _size;

    public PooledStreamStack(int size)
    {
        _array = new StreamAsValueType[size];
        _size = 0;
    }

    public readonly int Count => _size;

    public bool TryPop([NotNullWhen(true)] out TValue? result)
    {
        int size = _size - 1;
        StreamAsValueType[] array = _array;

        if ((uint)size >= (uint)array.Length)
        {
            result = default;
            return false;
        }

        _size = size;
        result = array[size];
        array[size] = default;
        return true;
    }

    public bool TryPeek([NotNullWhen(true)] out TValue? result)
    {
        int size = _size - 1;
        StreamAsValueType[] array = _array;

        if ((uint)size >= (uint)array.Length)
        {
            result = default;
            return false;
        }

        result = array[size];
        return true;
    }

    // Pushes an item to the top of the stack.
    public void Push(TValue item)
    {
        int size = _size;
        StreamAsValueType[] array = _array;

        if ((uint)size < (uint)array.Length)
        {
            array[size] = item;
            _size = size + 1;
        }
        else
        {
            PushWithResize(item);
        }
    }

    // Non-inline from Stack.Push to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PushWithResize(TValue item)
    {
        Array.Resize(ref _array, 2 * _array.Length);
        _array[_size] = item;
        _size++;
    }

    public void RemoveExpired(long timestamp)
    {
        int size = _size;
        StreamAsValueType[] array = _array;

        var removeCount = CalculateRemoveCount(timestamp, size, array);
        if (removeCount == 0)
        {
            return;
        }

        var newSize = size - removeCount;

        // Dispose removed streams
        for (var i = 0; i < removeCount; i++)
        {
            TValue stream = array[i];
            stream.DisposeCore();
        }

        // Move remaining streams
        for (var i = 0; i < newSize; i++)
        {
            array[i] = array[i + removeCount];
        }

        // Clear unused array indexes
        for (var i = newSize; i < size; i++)
        {
            array[i] = default;
        }

        _size = newSize;
    }

    private static int CalculateRemoveCount(long timestamp, int size, StreamAsValueType[] array)
    {
        for (var i = 0; i < size; i++)
        {
            TValue stream = array[i];
            if (stream.PoolExpirationTimestamp >= timestamp)
            {
                // Stream is still valid. All streams after this will have a later expiration.
                // No reason to keep checking. Return count of streams to remove.
                return i;
            }
        }

        // All will be removed.
        return size;
    }

    // See https://github.com/dotnet/runtime/blob/da9b16f2804e87c9c1ca9dcd9036e7b53e724f5d/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/BufferSegmentStack.cs#L68-L79
    internal readonly struct StreamAsValueType
    {
        private readonly TValue _value;
        private StreamAsValueType(TValue value) => _value = value;
        public static implicit operator StreamAsValueType(TValue s) => new StreamAsValueType(s);
        public static implicit operator TValue(StreamAsValueType s) => s._value;
    }
}
