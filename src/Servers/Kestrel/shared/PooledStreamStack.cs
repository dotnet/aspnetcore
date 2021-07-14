// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel
{
    internal interface IPooledStream
    {
        long PoolExpirationTicks { get; }
        void DisposeCore();
    }

    // See https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/BufferSegmentStack.cs
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

        public int Count => _size;

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

        public void RemoveExpired(long now)
        {
            int size = _size;
            StreamAsValueType[] array = _array;

            var removeCount = CalculateRemoveCount(now, size, array);
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

        private static int CalculateRemoveCount(long now, int size, StreamAsValueType[] array)
        {
            for (var i = 0; i < size; i++)
            {
                TValue stream = array[i];
                if (stream.PoolExpirationTicks >= now)
                {
                    // Stream is still valid. All streams after this will have a later expiration.
                    // No reason to keep checking. Return count of streams to remove.
                    return i;
                }
            }

            // All will be removed.
            return size;
        }

        internal readonly struct StreamAsValueType
        {
            private readonly TValue _value;
            private StreamAsValueType(TValue value) => _value = value;
            public static implicit operator StreamAsValueType(TValue s) => new StreamAsValueType(s);
            public static implicit operator TValue(StreamAsValueType s) => s._value;
        }
    }
}
