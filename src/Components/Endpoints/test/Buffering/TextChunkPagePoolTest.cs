// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class TextChunkPagePoolTest
{
    private readonly TestSharedPool<TextChunk> _sharedPool = new TestSharedPool<TextChunk>();

    [Fact]
    public void RentsFromSharedPool_ButDoesNotReturnToSharedPoolBeforeDisposal()
    {
        var subject = new TextChunkPagePool(_sharedPool, 123);
        var page = subject.Rent();

        // Rent
        Assert.Equal(123, page.Buffer.Length);
        Assert.Equal(0, page.Count);
        Assert.Single(_sharedPool.UnreturnedValues);

        // Mutate
        Assert.True(page.TryAdd(new TextChunk("Hello")));
        Assert.Equal(1, page.Count);

        // Return: does not return to shared pool before disposal
        subject.Return(page);
        Assert.Equal(0, page.Count);
        Assert.Single(_sharedPool.UnreturnedValues);
    }

    [Fact]
    public void ReusesReturnedItemsBypassingSharedPool()
    {
        // Rent two items
        var subject = new TextChunkPagePool(_sharedPool, 123);
        var page1 = subject.Rent();
        var page2 = subject.Rent();
        Assert.NotSame(page1.Buffer, page2.Buffer);
        Assert.True(page1.TryAdd(new TextChunk("Some value")));

        // Return only one; see neither was returned to the shared pool
        subject.Return(page1);
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        // Ask for a third and see we got the previously returned item
        var page3 = subject.Rent();
        Assert.Same(page1.Buffer, page3.Buffer);
        Assert.Equal(0, page3.Count);
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);
    }

    [Fact]
    public void ReturnsToSharedPoolOnDisposal()
    {
        // Rent two items
        var subject = new TextChunkPagePool(_sharedPool, 123);
        var page1 = subject.Rent();
        var page2 = subject.Rent();
        Assert.NotSame(page1.Buffer, page2.Buffer);

        // Return only one; see neither was returned to the shared pool
        subject.Return(page1);
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        // On disposal, both are returned
        subject.Dispose();
        Assert.Empty(_sharedPool.UnreturnedValues);
    }

    class TestSharedPool<T> : ArrayPool<T>
    {
        public List<T[]> UnreturnedValues { get; } = new();

        public override T[] Rent(int minimumLength)
        {
            var result = new T[minimumLength];
            UnreturnedValues.Add(result);
            return result;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (!UnreturnedValues.Remove(array))
            {
                throw new InvalidOperationException("Tried to return a value not previously rented.");
            }
        }
    }
}
