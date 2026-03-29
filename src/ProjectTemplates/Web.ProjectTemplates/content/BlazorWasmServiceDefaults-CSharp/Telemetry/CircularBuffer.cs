// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorWasm.ServiceDefaults1.Telemetry;

internal sealed class CircularBuffer<T>
{
    private readonly T?[] _buffer;
    private readonly int _capacity;
    private readonly object _lock = new();
    private int _head;
    private int _tail;
    private int _count;

    public CircularBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new T?[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    public bool TryAdd(T item)
    {
        lock (_lock)
        {
            if (_count >= _capacity)
            {
                return false;
            }

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;
            _count++;
            return true;
        }
    }

    public bool TryTake(out T? item)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default;
            _head = (_head + 1) % _capacity;
            _count--;
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _capacity);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
