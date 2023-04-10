// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Forms;

// In ReverseStringBuilder, we use a 'fixed' array to track the buffers
// we use to build the string. However, fixed arrays cannot contain managed types,
// so we instead store integer keys that we can use to fetch the actual buffers.
// This type serves as a memory pool whose rented memory can be fetched and returned
// using integer keys.
internal sealed class IntKeyedMemoryPool<T>
{
    public static readonly IntKeyedMemoryPool<T> Shared = new();

    private readonly List<RentedMemory> _pool = new();

    private int _nextFree = -1;

    public int Rent(int length, out Memory<T> result)
    {
        var array = ArrayPool<T>.Shared.Rent(length);
        int id;

        lock (this)
        {
            if (_nextFree < 0)
            {
                id = _pool.Count;
                var rentedMemory = new RentedMemory(id, array, length);
                _pool.Add(rentedMemory);
            }
            else
            {
                id = _nextFree;
                _nextFree = _pool[_nextFree].Id;
                var rentedMemory = new RentedMemory(id, array, length);
                _pool[id] = rentedMemory;
            }
        }

        result = new(array, 0, length);
        return id;
    }

    public Memory<T> GetRentedMemory(int id)
    {
        RentedMemory rentedMemory;

        lock (this)
        {
            rentedMemory = GetRentedMemoryCore(id);
        }

        return new(rentedMemory.Array, 0, rentedMemory.Length);
    }

    public void Return(int id)
    {
        RentedMemory rentedMemory;

        lock (this)
        {
            rentedMemory = GetRentedMemoryCore(id);
            _pool[id] = rentedMemory with { Id = _nextFree };
            _nextFree = id;
        }

        ArrayPool<T>.Shared.Return(rentedMemory.Array);
    }

    // This method assumes it's being called from within a 'lock' statement.
    private RentedMemory GetRentedMemoryCore(int id)
    {
        if (id < 0 || id > _pool.Count - 1)
        {
            throw UnknownMemoryIdException(id);
        }

        var rentedMemory = _pool[id];
        if (rentedMemory.Id != id)
        {
            // The memory has already been returned to the pool and is now part of the
            // "free list".
            throw UnknownMemoryIdException(id);
        }

        return rentedMemory;

        static InvalidOperationException UnknownMemoryIdException(int id)
            => new($"Unknown rented memory ID '{id}'.");
    }

    // If 'Id' is different than the RentedMemory's index in the pool, it acts
    // as a pointer to the next 'free' RentedMemory.
    private readonly record struct RentedMemory(int Id, T[] Array, int Length);
}
