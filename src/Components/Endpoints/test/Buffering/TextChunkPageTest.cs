// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class TextChunkPageTest
{
    [Fact]
    public async Task CanAddValuesUntilFull()
    {
        // Arrange
        var page = new TextChunkPage(3);
        var charArrayScope = new StringBuilder();

        // Act/Assert 1: Can add values if not full
        Assert.Equal(0, page.Count);
        Assert.True(page.TryAdd(new TextChunk("Item1")));
        Assert.True(page.TryAdd(new TextChunk("Item2")));
        Assert.True(page.TryAdd(new TextChunk("Item3")));
        Assert.Equal(3, page.Count);

        // Act/Assert 2: Can't add values if full
        Assert.False(page.TryAdd(new TextChunk("Item4")));
        Assert.Equal(3, page.Count);

        // Assert: Got the expected contents
        var writer = new StringWriter();
        StringBuilder tempBuffer = null;
        for (var i = 0; i < page.Count; i++)
        {
            await page.Buffer[i].WriteToAsync(writer, charArrayScope.ToString(), ref tempBuffer);
        }

        Assert.Equal("Item1Item2Item3", writer.ToString());
    }
}
