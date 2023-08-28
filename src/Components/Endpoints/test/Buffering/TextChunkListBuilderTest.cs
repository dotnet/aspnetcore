// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class TextChunkListBuilderTest
{
    private readonly TestSharedPool<TextChunk> _sharedPool = new TestSharedPool<TextChunk>();

    [Fact]
    public async Task CanAddContentThatSpansMultiplePages()
    {
        var builder = new TextChunkListBuilder(_sharedPool, pageLength: 2);

        // Populate first page
        builder.Add(new TextChunk("Item1"));
        Assert.Single(_sharedPool.UnreturnedValues);
        builder.Add(new TextChunk("Item2"));
        Assert.Single(_sharedPool.UnreturnedValues);

        // Part-populate second page
        builder.Add(new TextChunk("Item3"));
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        // Assert: has expected content
        var writer1 = new StringWriter();
        await builder.WriteToAsync(writer1);
        Assert.Equal("Item1Item2Item3", writer1.ToString());

        // Clearing does clear, but doesn't return pages to the shared pool
        builder.Clear();
        var writer2 = new StringWriter();
        await builder.WriteToAsync(writer2);
        Assert.Equal("", writer2.ToString());
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        // On reuse, it reuses pages without getting more from the shared pool
        builder.Add(new TextChunk("Item4"));
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        var writer3 = new StringWriter();
        await builder.WriteToAsync(writer3);
        Assert.Equal("Item4", writer3.ToString());
    }

    [Fact]
    public void DisposalReturnsAllPagesToSharedPool()
    {
        var builder = new TextChunkListBuilder(_sharedPool, pageLength: 2);

        // Populate first page
        builder.Add(new TextChunk("Item1"));
        Assert.Single(_sharedPool.UnreturnedValues);
        builder.Add(new TextChunk("Item2"));
        Assert.Single(_sharedPool.UnreturnedValues);

        // Part-populate second page
        builder.Add(new TextChunk("Item3"));
        Assert.Equal(2, _sharedPool.UnreturnedValues.Count);

        // Dispose
        builder.Dispose();
        Assert.Empty(_sharedPool.UnreturnedValues);
    }
}
