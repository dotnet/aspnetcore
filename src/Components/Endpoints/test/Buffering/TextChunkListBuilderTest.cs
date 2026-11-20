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
        builder.Add(new[] { 'I', 't', 'e', 'm', '2' });

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
        builder.Add("Item5".AsSpan());

        var writer3 = new StringWriter();
        await builder.WriteToAsync(writer3);
        Assert.Equal("Item4Item5", writer3.ToString());
    }

    [Fact]
    public async Task CoalescesAdjacentCopiedText()
    {
        var builder = new TextChunkListBuilder(pageLength: 2);
        var chars = new[] { 'a', 'b', 'c' };

        builder.Add(chars);
        builder.Add("de".AsSpan());
        builder.Add(new TextChunk("F"));
        builder.Add("gh".AsSpan());

        // The copied text must not be affected by later changes to the source buffer
        chars[0] = 'X';

        var writer = new CountingStringWriter();
        await builder.WriteToAsync(writer);
        Assert.Equal("abcdeFgh", writer.ToString());
        Assert.Equal(3, writer.WriteCount);
    }

    [Fact]
    public async Task CanGrowCopiedTextBuffer()
    {
        var builder = new TextChunkListBuilder(pageLength: 2);
        var expected = new System.Text.StringBuilder();
        for (var i = 0; i < 1000; i++)
        {
            var value = $"[{i}]";
            builder.Add(value.AsSpan());
            expected.Append(value);
            if (i % 100 == 0)
            {
                builder.Add(new TextChunk("|"));
                expected.Append('|');
            }
        }

        var writer = new StringWriter();
        await builder.WriteToAsync(writer);
        Assert.Equal(expected.ToString(), writer.ToString());

        // Can write text larger than a single page
        var large = new string('L', 5000);
        builder.Add(large.AsSpan());
        expected.Append(large);
        var writer3 = new StringWriter();
        await builder.WriteToAsync(writer3);
        Assert.Equal(expected.ToString(), writer3.ToString());

        // Can reuse after clearing
        builder.Clear();
        builder.Add("reused".AsSpan());
        var writer2 = new StringWriter();
        await builder.WriteToAsync(writer2);
        Assert.Equal("reused", writer2.ToString());
    }

    private sealed class CountingStringWriter : StringWriter
    {
        public int WriteCount { get; private set; }

        public override Task WriteAsync(string value)
        {
            WriteCount++;
            return base.WriteAsync(value);
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            WriteCount++;
            return base.WriteAsync(buffer, cancellationToken);
        }
    }
}
