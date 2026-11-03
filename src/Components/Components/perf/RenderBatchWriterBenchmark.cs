// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Microsoft.AspNetCore.Components.Performance;

public class RenderBatchWriterBenchmark
{
    private RenderBatch _largeBatch;
    private RenderBatch _smallBatch;
    private Server.Circuits.ArrayBuilder<byte> _output;

    [GlobalSetup]
    public void Setup()
    {
        // Large batch: initial render of a 200-row table, inserted with a single PrependFrame edit per row.
        var builder = new RenderTreeBuilder();
        for (var i = 0; i < 200; i++)
        {
            builder.OpenElement(0, "tr");
            builder.AddAttribute(1, "class", i % 2 == 0 ? "even" : "odd");
            builder.OpenElement(2, "td");
            builder.AddContent(3, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.CloseElement();
            builder.OpenElement(4, "td");
            builder.AddContent(5, "Item number " + i);
            builder.CloseElement();
            builder.OpenElement(6, "td");
            builder.OpenElement(7, "button");
            builder.AddAttribute(8, "type", "button");
            builder.AddAttribute(9, "disabled", i % 3 == 0);
            builder.AddContent(10, "Delete");
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }
        var frames = builder.GetFrames();
        var edits = new List<RenderTreeEdit>();
        for (var i = 0; i < frames.Count; i++)
        {
            if (frames.Array[i].FrameType == RenderTreeFrameType.Element && frames.Array[i].ElementName == "tr")
            {
                edits.Add(RenderTreeEdit.PrependFrame(edits.Count, i));
            }
        }
        var diffs = new[] { new RenderTreeDiff(1, ToSegment(edits)) };
        _largeBatch = new RenderBatch(
            new ArrayRange<RenderTreeDiff>(diffs, diffs.Length),
            frames,
            default,
            default,
            default);

        // Small batch: typical interactive update (counter-style text change + attribute change).
        var smallBuilder = new RenderTreeBuilder();
        smallBuilder.OpenElement(0, "button");
        smallBuilder.AddAttribute(1, "class", "btn btn-primary active");
        smallBuilder.AddContent(2, "Current count: 42");
        smallBuilder.CloseElement();
        var smallFrames = smallBuilder.GetFrames();
        var smallEdits = new[]
        {
            RenderTreeEdit.StepIn(2),
            RenderTreeEdit.UpdateText(0, 2),
            RenderTreeEdit.StepOut(),
            RenderTreeEdit.SetAttribute(3, 1),
            RenderTreeEdit.RemoveAttribute(3, "disabled"),
        };
        var smallDiffs = new[] { new RenderTreeDiff(1, ToSegment(smallEdits)) };
        _smallBatch = new RenderBatch(
            new ArrayRange<RenderTreeDiff>(smallDiffs, smallDiffs.Length),
            smallFrames,
            default,
            default,
            default);

        _output = new Server.Circuits.ArrayBuilder<byte>(2048);
    }

    private static ArrayBuilderSegment<RenderTreeEdit> ToSegment(IEnumerable<RenderTreeEdit> edits)
    {
        var editsBuilder = new RenderTree.ArrayBuilder<RenderTreeEdit>();
        foreach (var edit in edits)
        {
            editsBuilder.Append(edit);
        }
        return editsBuilder.ToSegment(0, editsBuilder.Count);
    }

    [GlobalCleanup]
    public void Cleanup() => _output.Dispose();

    [Benchmark]
    public int WriteLargeBatch() => Write(in _largeBatch);

    [Benchmark]
    public int WriteSmallBatch() => Write(in _smallBatch);

    private int Write(in RenderBatch batch)
    {
        _output.Clear();
        using var stream = new ArrayBuilderMemoryStream(_output);
        using (var writer = new RenderBatchWriter(stream, false))
        {
            writer.Write(in batch);
        }
        return _output.Count;
    }
}
