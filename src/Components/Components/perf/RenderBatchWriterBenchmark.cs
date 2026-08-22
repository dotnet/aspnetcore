// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Microsoft.AspNetCore.Components.Performance;

/// <summary>
/// Measures the cost of serializing a <see cref="RenderBatch"/> reference-frame
/// section through <see cref="RenderBatchWriter"/>. The ref-based <c>Write</c>
/// overload avoids the per-property defensive copies that the <c>in</c>-based
/// overload triggered inside the JIT-compiled body.
/// </summary>
[MemoryDiagnoser]
public class RenderBatchWriterBenchmark
{
    // A handful of representative batch sizes — small, medium, large. Real-world
    // batches for a typical Blazor Server page tend to be on the order of a few
    // hundred reference frames.
    [Params(64, 512, 4096)]
    public int ReferenceFrameCount { get; set; }

    private RenderBatch _batch = default!;
    private MemoryStream _output = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Build a frame array that exercises a representative set of RenderTreeFrameType values
        // so the benchmark is closer to real workloads (and so it traverses multiple branches
        // in the switch in Write).
        var frames = new RenderTreeFrame[ReferenceFrameCount];
        for (var i = 0; i < frames.Length; i++)
        {
            frames[i] = (i % 12) switch
            {
                0 => RenderTreeFrame.Attribute(i, $"attr-{i}", $"value-{i}"),
                1 => RenderTreeFrame.Element(i, $"element-{i}").WithElementSubtreeLength(2),
                2 => RenderTreeFrame.Text(i, $"text-{i}"),
                3 => RenderTreeFrame.Markup(i, $"<markup-{i}/>"),
                4 => RenderTreeFrame.Region(i).WithRegionSubtreeLength(1),
                5 => RenderTreeFrame.ElementReferenceCapture(i, _ => { }).WithElementReferenceCaptureId($"id-{i}"),
                6 => RenderTreeFrame.NamedEvent(i, "SomeEventType", $"name-{i}"),
                7 => RenderTreeFrame.ComponentRenderModeFrame(i, Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveAuto),
                8 => RenderTreeFrame.Attribute(i, $"attr-{i}", true),
                9 => RenderTreeFrame.Text(i, " "),
                10 => RenderTreeFrame.Markup(i, " "),
                _ => RenderTreeFrame.Attribute(i, $"attr-{i}", $"value-{i}")
            };
        }

        _batch = new RenderBatch(
            default,
            new ArrayRange<RenderTreeFrame>(frames, frames.Length),
            default,
            default,
            default);

        // A reused output stream. We only need the throughput signal — the
        // exact byte contents are not consumed by the benchmark.
        _output = new MemoryStream(capacity: 64 * 1024);
    }

    [Benchmark(Description = "RenderBatchWriter: serialize N reference frames.")]
    public int WriteReferenceFrames()
    {
        _output.Position = 0;
        _output.SetLength(0);

        using (var writer = new RenderBatchWriter(_output, leaveOpen: true))
        {
            writer.Write(_batch);
        }

        return (int)_output.Length;
    }
}
