// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class TextChunkListBuilderTest
{
    [Fact]
    public async Task CanAddContentThatSpansMultiplePages()
    {
        var builder = new TextChunkListBuilder(pageLength: 2);

        // Populate first page
        builder.Add(new TextChunk("Item1"));
        builder.Add(new TextChunk("Item2"));

        // Part-populate second page
        builder.Add(new TextChunk("Item3"));

        // Assert: has expected content
        var writer1 = new StringWriter();
        await builder.WriteToAsync(writer1);
        Assert.Equal("Item1Item2Item3", writer1.ToString());

        // Clearing works
        builder.Clear();
        var writer2 = new StringWriter();
        await builder.WriteToAsync(writer2);
        Assert.Equal("", writer2.ToString());

        // Can then reuse
        builder.Add(new TextChunk("Item4"));

        var writer3 = new StringWriter();
        await builder.WriteToAsync(writer3);
        Assert.Equal("Item4", writer3.ToString());
    }
}
