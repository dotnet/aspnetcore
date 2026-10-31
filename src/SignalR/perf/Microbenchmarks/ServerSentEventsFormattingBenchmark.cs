// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class ServerSentEventsFormattingBenchmark
{
    private ReadOnlySequence<byte> _payload;

    [Params(32, 4096)]
    public int LineLength;

    [Params(1, 16)]
    public int LineCount;

    [Params(false, true)]
    public bool Segmented;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _payload = CreatePayload(LineLength, LineCount, Segmented, out var expected);

        using var output = new MemoryStream();
        ServerSentEventsMessageFormatter.WriteMessageAsync(_payload, output, default).GetAwaiter().GetResult();
        if (!output.ToArray().AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException("The formatted SSE payload does not match the expected framing.");
        }
    }

    internal static ReadOnlySequence<byte> CreatePayload(int lineLength, int lineCount, bool segmented, out byte[] expectedOutput)
    {
        var line = new string('x', lineLength);
        var payload = new StringBuilder();
        var expected = new StringBuilder();
        for (var i = 0; i < lineCount; i++)
        {
            if (i > 0)
            {
                payload.Append("\r\n");
            }

            payload.Append(line);
            expected.Append("data: ").Append(line).Append("\r\n");
        }
        expected.Append("\r\n");
        expectedOutput = Encoding.UTF8.GetBytes(expected.ToString());

        var bytes = Encoding.UTF8.GetBytes(payload.ToString());
        if (segmented)
        {
            // Split the first CRLF across segments for multiline payloads.
            var split = lineCount > 1 ? lineLength + 1 : lineLength / 2;
            var first = new BufferSegment(bytes.AsMemory(0, split));
            var last = first.Append(bytes.AsMemory(split));
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        return new ReadOnlySequence<byte>(bytes);
    }

    [Benchmark]
    public Task WriteMessage()
    {
        return ServerSentEventsMessageFormatter.WriteMessageAsync(_payload, Stream.Null, default);
    }
}
