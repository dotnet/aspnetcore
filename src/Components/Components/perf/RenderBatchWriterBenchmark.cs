// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Performance;

[MemoryDiagnoser]
public class RenderBatchWriterBenchmark
{
    [Params(64, 512, 4096)]
    public int ReferenceFrameCount { get; set; }

    private RenderTreeFrame[] _frames = default!;
    private MemoryStream _output = default!;

    [GlobalSetup]
    public void Setup()
    {
        _frames = new RenderTreeFrame[ReferenceFrameCount];
        for (var i = 0; i < _frames.Length; i++)
        {
            _frames[i] = (i % 12) switch
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

        _output = new MemoryStream(capacity: 64 * 1024);
    }

    [Benchmark(Baseline = true, Description = "in RenderTreeFrame (pre-PR)")]
    public int WriteFrames_In()
    {
        _output.Position = 0;
        _output.SetLength(0);
        using var w = new FramePassingWriter(_output);
        w.WriteLoop_In(_frames, _frames.Length);
        return (int)_output.Length;
    }

    [Benchmark(Description = "ref array[i] (current production)")]
    public int WriteFrames_DirectRef()
    {
        _output.Position = 0;
        _output.SetLength(0);
        using var w = new FramePassingWriter(_output);
        w.WriteLoop_DirectRef(_frames, _frames.Length);
        return (int)_output.Length;
    }

    [Benchmark(Description = "local copy + ref")]
    public int WriteFrames_LocalCopyRef()
    {
        _output.Position = 0;
        _output.SetLength(0);
        using var w = new FramePassingWriter(_output);
        w.WriteLoop_LocalCopyRef(_frames, _frames.Length);
        return (int)_output.Length;
    }

    [Benchmark(Description = "by-value RenderTreeFrame")]
    public int WriteFrames_ByValue()
    {
        _output.Position = 0;
        _output.SetLength(0);
        using var w = new FramePassingWriter(_output);
        w.WriteLoop_ByValue(_frames, _frames.Length);
        return (int)_output.Length;
    }
    private sealed class FramePassingWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly Dictionary<string, int> _deduplicatedStringIndices = new();
        private readonly List<string> _strings = new();

        public FramePassingWriter(Stream stream)
            => _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        public void Dispose() => _writer.Dispose();
        public void WriteLoop_DirectRef(RenderTreeFrame[] array, int count)
        {
            _writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                WriteFrame(ref array[i]);
            }
        }
        public void WriteLoop_LocalCopyRef(RenderTreeFrame[] array, int count)
        {
            _writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                var f = array[i];
                WriteFrame(ref f);
            }
        }

        public void WriteLoop_ByValue(RenderTreeFrame[] array, int count)
        {
            _writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                WriteFrameByValue(array[i]);
            }
        }

        public void WriteLoop_In(RenderTreeFrame[] array, int count)
        {
            _writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                WriteFrameIn(in array[i]);
            }
        }

        private void WriteFrame(ref RenderTreeFrame frame)
        {
            _writer.Write((int)frame.FrameType);
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    WriteString(frame.AttributeName, allowDeduplication: true);
                    if (frame.AttributeValue is bool boolValue)
                    {
                        WriteString(boolValue ? string.Empty : null, allowDeduplication: true);
                    }
                    else
                    {
                        var attrString = frame.AttributeValue as string;
                        WriteString(attrString, allowDeduplication: string.IsNullOrEmpty(attrString));
                    }
                    _writer.Write(frame.AttributeEventHandlerId);
                    break;
                case RenderTreeFrameType.Component:
                    _writer.Write(frame.ComponentSubtreeLength);
                    _writer.Write(frame.ComponentId);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                case RenderTreeFrameType.ComponentRenderMode:
                case RenderTreeFrameType.NamedEvent:
                    WritePadding(16);
                    break;
                case RenderTreeFrameType.Element:
                    _writer.Write(frame.ElementSubtreeLength);
                    WriteString(frame.ElementName, allowDeduplication: true);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    WriteString(frame.ElementReferenceCaptureId, allowDeduplication: false);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Region:
                    _writer.Write(frame.RegionSubtreeLength);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Text:
                    WriteString(frame.TextContent, allowDeduplication: string.IsNullOrWhiteSpace(frame.TextContent));
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Markup:
                    WriteString(frame.MarkupContent, allowDeduplication: false);
                    WritePadding(12);
                    break;
                default:
                    throw new ArgumentException($"Unsupported frame type: {frame.FrameType}");
            }
        }
        private void WriteFrameByValue(RenderTreeFrame frame)
        {
            _writer.Write((int)frame.FrameType);
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    WriteString(frame.AttributeName, allowDeduplication: true);
                    if (frame.AttributeValue is bool boolValue)
                    {
                        WriteString(boolValue ? string.Empty : null, allowDeduplication: true);
                    }
                    else
                    {
                        var attrString = frame.AttributeValue as string;
                        WriteString(attrString, allowDeduplication: string.IsNullOrEmpty(attrString));
                    }
                    _writer.Write(frame.AttributeEventHandlerId);
                    break;
                case RenderTreeFrameType.Component:
                    _writer.Write(frame.ComponentSubtreeLength);
                    _writer.Write(frame.ComponentId);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                case RenderTreeFrameType.ComponentRenderMode:
                case RenderTreeFrameType.NamedEvent:
                    WritePadding(16);
                    break;
                case RenderTreeFrameType.Element:
                    _writer.Write(frame.ElementSubtreeLength);
                    WriteString(frame.ElementName, allowDeduplication: true);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    WriteString(frame.ElementReferenceCaptureId, allowDeduplication: false);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Region:
                    _writer.Write(frame.RegionSubtreeLength);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Text:
                    WriteString(frame.TextContent, allowDeduplication: string.IsNullOrWhiteSpace(frame.TextContent));
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Markup:
                    WriteString(frame.MarkupContent, allowDeduplication: false);
                    WritePadding(12);
                    break;
                default:
                    throw new ArgumentException($"Unsupported frame type: {frame.FrameType}");
            }
        }
        private void WriteFrameIn(in RenderTreeFrame frame)
        {
            _writer.Write((int)frame.FrameType);
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    WriteString(frame.AttributeName, allowDeduplication: true);
                    if (frame.AttributeValue is bool boolValue)
                    {
                        WriteString(boolValue ? string.Empty : null, allowDeduplication: true);
                    }
                    else
                    {
                        var attrString = frame.AttributeValue as string;
                        WriteString(attrString, allowDeduplication: string.IsNullOrEmpty(attrString));
                    }
                    _writer.Write(frame.AttributeEventHandlerId);
                    break;
                case RenderTreeFrameType.Component:
                    _writer.Write(frame.ComponentSubtreeLength);
                    _writer.Write(frame.ComponentId);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                case RenderTreeFrameType.ComponentRenderMode:
                case RenderTreeFrameType.NamedEvent:
                    WritePadding(16);
                    break;
                case RenderTreeFrameType.Element:
                    _writer.Write(frame.ElementSubtreeLength);
                    WriteString(frame.ElementName, allowDeduplication: true);
                    WritePadding(8);
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    WriteString(frame.ElementReferenceCaptureId, allowDeduplication: false);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Region:
                    _writer.Write(frame.RegionSubtreeLength);
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Text:
                    WriteString(frame.TextContent, allowDeduplication: string.IsNullOrWhiteSpace(frame.TextContent));
                    WritePadding(12);
                    break;
                case RenderTreeFrameType.Markup:
                    WriteString(frame.MarkupContent, allowDeduplication: false);
                    WritePadding(12);
                    break;
                default:
                    throw new ArgumentException($"Unsupported frame type: {frame.FrameType}");
            }
        }

        private void WriteString(string? value, bool allowDeduplication)
        {
            if (value is null)
            {
                _writer.Write(-1);
            }
            else
            {
                int stringIndex;
                if (!allowDeduplication || !_deduplicatedStringIndices.TryGetValue(value, out stringIndex))
                {
                    stringIndex = _strings.Count;
                    _strings.Add(value);
                    if (allowDeduplication)
                    {
                        _deduplicatedStringIndices.Add(value, stringIndex);
                    }
                }
                _writer.Write(stringIndex);
            }
        }

        private void WritePadding(int numBytes)
        {
            while (numBytes >= 4)
            {
                _writer.Write(0);
                numBytes -= 4;
            }
            while (numBytes > 0)
            {
                _writer.Write((byte)0);
                numBytes--;
            }
        }
    }
}
