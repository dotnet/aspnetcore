// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class TextChunkListBuilderTest
{
    [Fact]
    public async Task CanAddContentThatSpansMultiplePages()
    {
        var builder = new TextChunkListBuilder(pageLength: 2);
        var charArrayScope = new StringBuilder();

        // Populate first page
        builder.Add(new TextChunk("Item1"));
        builder.Add(new TextChunk(new[] { 'I', 't', 'e', 'm', '2' }, charArrayScope));

        // Part-populate second page
        builder.Add(new TextChunk("Item3"));

        // Assert: has expected content
        var writer1 = new StringWriter();
        await builder.WriteToAsync(writer1, charArrayScope.ToString());
        Assert.Equal("Item1Item2Item3", writer1.ToString());

        // Clearing works
        builder.Clear();
        var writer2 = new StringWriter();
        await builder.WriteToAsync(writer2, charArrayScope.ToString());
        Assert.Equal("", writer2.ToString());

        // Can then reuse
        builder.Add(new TextChunk("Item4"));

        var writer3 = new StringWriter();
        await builder.WriteToAsync(writer3, charArrayScope.ToString());
        Assert.Equal("Item4", writer3.ToString());
    }
}
