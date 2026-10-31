// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Endpoints.Rendering;

namespace Microsoft.AspNetCore.Components.Performance;

// Simulates static SSR output: HTML-encoded text written through BufferedTextWriter,
// flushed once (non-streaming) or once per batch (streaming rendering).
public class BufferedTextWriterBenchmark
{
    private readonly HtmlEncoder _encoder = HtmlEncoder.Default;
    private readonly string[] _values = new string[100];

    [Params(1, 50)]
    public int Flushes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < _values.Length; i++)
        {
            // Mix of text that does and doesn't require encoding.
            _values[i] = i % 2 == 0 ? $"Plain text value {i}" : $"Tom & Jerry's <item {i}> \"quoted\"";
        }
    }

    [Benchmark]
    public async Task WriteAndFlush()
    {
        using var writer = new BufferedTextWriter(TextWriter.Null);
        for (var f = 0; f < Flushes; f++)
        {
            foreach (var value in _values)
            {
                writer.Write("<td>");
                _encoder.Encode(writer, value);
                writer.Write("</td>");
            }

            await writer.FlushAsync();
        }
    }
}
