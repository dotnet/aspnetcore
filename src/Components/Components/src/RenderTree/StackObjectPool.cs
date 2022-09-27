// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

// This is a very simple object pool that requires Get and Return calls to be
// balanced as in a stack. It retains up to 'maxPreservedItems' instances in
// memory, then for any further requests it supplies untracked instances.

internal sealed class StackObjectPool<T> where T : class
{
    private readonly int _maxPreservedItems;
    private readonly Func<T> _instanceFactory;
    private readonly T[] _contents;
    private int _numSuppliedItems;
    private int _numTrackedItems;

    public StackObjectPool(int maxPreservedItems, Func<T> instanceFactory)
    {
        _maxPreservedItems = maxPreservedItems;
        _instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
        _contents = new T[_maxPreservedItems];
    }

    public T Get()
    {
        _numSuppliedItems++;

        if (_numSuppliedItems <= _maxPreservedItems)
        {
            if (_numTrackedItems < _numSuppliedItems)
            {
                // Need to allocate a new one
                var newItem = _instanceFactory();
                _contents[_numTrackedItems++] = newItem;
                return newItem;
            }
            else
            {
                // Can use one that's already in the pool
                return _contents[_numSuppliedItems - 1];
            }
        }
        else
        {
            // Pool is full; return untracked instance
            return _instanceFactory();
        }
    }

    public void Return(T instance)
    {
        if (_numSuppliedItems <= 0)
        {
            throw new InvalidOperationException("There are no outstanding instances to return.");
        }
        else if (_numSuppliedItems <= _maxPreservedItems)
        {
            // We check you're returning the right instance only as a way of
            // catching Get/Return mismatch bugs
            var expectedInstance = _contents[_numSuppliedItems - 1];
            if (!ReferenceEquals(instance, expectedInstance))
            {
                throw new ArgumentException($"Attempting to return wrong pooled instance. {nameof(Get)}/{nameof(Return)} calls must form a stack.");
            }
        }

        // It's a valid call. Track that we're no longer "supplying" the top item,
        // but keep the instance in the _contents array for future reuse.
        _numSuppliedItems--;
    }
}
