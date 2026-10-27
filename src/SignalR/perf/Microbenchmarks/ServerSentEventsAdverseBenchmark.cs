// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class ServerSentEventsAdverseBenchmark
{
    private ReadOnlySequence<byte> _payload;

    [Params("Tiny", "DenseNewlines", "Fragmented", "LateNewline")]
    public string Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var text = Scenario switch
        {
            "Tiny" => "a\nb",
            "DenseNewlines" => new string('\n', 8190),
            "Fragmented" => new string('x', 4096) + "\ny",
            "LateNewline" => new string('x', 65535) + "\n",
            _ => throw new InvalidOperationException("Unknown scenario."),
        };
        var bytes = Encoding.UTF8.GetBytes(text);
        _payload = new ReadOnlySequence<byte>(bytes);
        if (Scenario == "Fragmented")
        {
            var first = new BufferSegment(bytes.AsMemory(0, 1));
            var last = first;
            for (var i = 1; i < bytes.Length; i++)
            {
                last = last.Append(bytes.AsMemory(i, 1));
            }
            _payload = new ReadOnlySequence<byte>(first, 0, last, 1);
        }

        using var output = new MemoryStream();
        ServerSentEventsMessageFormatter.WriteMessageAsync(_payload, output, default).GetAwaiter().GetResult();
        var expected = string.Concat(text.Split('\n').Select(line => $"data: {line}\r\n")) + "\r\n";
        if (Encoding.UTF8.GetString(output.ToArray()) != expected)
        {
            throw new InvalidOperationException("SSE framing changed.");
        }
    }

    [Benchmark]
    public Task WriteMessage() => ServerSentEventsMessageFormatter.WriteMessageAsync(_payload, Stream.Null, default);
}
