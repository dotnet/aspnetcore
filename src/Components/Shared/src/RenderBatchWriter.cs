// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Components.RenderTree;

#if BLAZOR_WEBVIEW
namespace Microsoft.AspNetCore.Components.WebView;
#elif COMPONENTS_WEBASSEMBLY
namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;
#else
namespace Microsoft.AspNetCore.Components.Server.Circuits;
#endif

// TODO: We should consider *not* having this type of infrastructure in the .Server
// project, but instead in some new project called .Remote or similar, since it
// would also be used in Electron and possibly WebWorker cases.

/// <summary>
/// Provides a custom binary serializer for <see cref="RenderBatch"/> instances.
/// This is designed with both server-side and client-side perf in mind:
///
///  * Array-like regions always have a fixed size per entry (even if some entry types
///    don't require as much space as others) so the recipient can index directly.
///  * The indices describing where field data starts, where each string value starts,
///    etc., are written *after* that data, so when writing the data we don't have to
///    compute the locations up front or seek back to an earlier point in the stream.
///    The recipient can only process the data after reading it all into a buffer,
///    so it's no disadvantage for the location info to be at the end.
///  * We only serialize the data that the JS side will need. For example, we don't
///    emit frame sequence numbers, or any representation of nonstring attribute
///    values, or component instances, etc.
///
/// We don't have or need a .NET reader for this format. We only read it from JS code.
/// </summary>
internal sealed class RenderBatchWriter : IDisposable
{
    private readonly ArrayBuilder<string> _strings;
    private readonly Dictionary<string, int> _deduplicatedStringIndices;
    private readonly BinaryWriter _binaryWriter;
    private readonly bool _useUtf16StringTable;

    // Private scratch space used to assemble fixed-size records before writing them in a single call.
    // Every write fully overwrites the bytes it emits, so no data carries over between records or batches.
    private const int ScratchBufferSize = 256;
    private byte[]? _scratchBuffer = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);

    public RenderBatchWriter(Stream output, bool leaveOpen)
        : this(output, leaveOpen, useUtf16StringTable: false)
    {
    }

    public RenderBatchWriter(Stream output, bool leaveOpen, bool useUtf16StringTable)
    {
        _strings = new ArrayBuilder<string>();
        _deduplicatedStringIndices = new Dictionary<string, int>();
        _binaryWriter = new BinaryWriter(output, Encoding.UTF8, leaveOpen);
        _useUtf16StringTable = useUtf16StringTable;
    }

    public void Write(in RenderBatch renderBatch)
    {
        var updatedComponentsOffset = Write(renderBatch.UpdatedComponents);
        var referenceFramesOffset = Write(renderBatch.ReferenceFrames);
        var disposedComponentIdsOffset = Write(renderBatch.DisposedComponentIDs);
        var disposedEventHandlerIdsOffset = Write(renderBatch.DisposedEventHandlerIDs);
        var stringTableOffset = _useUtf16StringTable ? WriteStringTableUtf16() : WriteStringTable();

        _binaryWriter.Write(updatedComponentsOffset);
        _binaryWriter.Write(referenceFramesOffset);
        _binaryWriter.Write(disposedComponentIdsOffset);
        _binaryWriter.Write(disposedEventHandlerIdsOffset);
        _binaryWriter.Write(stringTableOffset);
    }

    int Write(in ArrayRange<RenderTreeDiff> diffs)
    {
        var count = diffs.Count;
        var diffsIndexes = new int[count];
        var array = diffs.Array;
        var baseStream = _binaryWriter.BaseStream;
        for (var i = 0; i < count; i++)
        {
            diffsIndexes[i] = (int)baseStream.Position;
            Write(array[i]);
        }

        // Now write out the table of locations
        var tableStartPos = (int)baseStream.Position;
        _binaryWriter.Write(count);
        for (var i = 0; i < count; i++)
        {
            _binaryWriter.Write(diffsIndexes[i]);
        }

        return tableStartPos;
    }

    void Write(in RenderTreeDiff diff)
    {
        _binaryWriter.Write(diff.ComponentId);

        var edits = diff.Edits;
        _binaryWriter.Write(edits.Count);

        var editsArray = edits.Array;
        var editsEndIndexExcl = edits.Offset + edits.Count;
        for (var i = edits.Offset; i < editsEndIndexExcl; i++)
        {
            Write(editsArray[i]);
        }
    }

    void Write(in RenderTreeEdit edit)
    {
        // We want all RenderTreeEdit outputs to be of the same length, so that
        // the recipient can index into the array directly without walking it.
        // So we output some value for all properties, even when not applicable
        // for this specific RenderTreeEditType.
        var buffer = _scratchBuffer.AsSpan(0, 16);
        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)edit.Type);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), edit.SiblingIndex);

        // ReferenceFrameIndex and MoveToSiblingIndex share a slot, so this writes
        // whichever one applies to the edit type
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8), edit.ReferenceFrameIndex);

        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12), GetStringIndex(edit.RemovedAttributeName, allowDeduplication: true));
        _binaryWriter.Write(buffer);
    }

    int Write(in ArrayRange<RenderTreeFrame> frames)
    {
        var startPos = (int)_binaryWriter.BaseStream.Position;

        var array = frames.Array;
        var count = frames.Count;
        _binaryWriter.Write(count);
        for (var i = 0; i < count; i++)
        {
            Write(array[i]);
        }

        return startPos;
    }

    void Write(in RenderTreeFrame frame)
    {
        // We want each frame to take up the same number of bytes, so that the
        // recipient can index into the array directly instead of having to
        // walk through it.
        // Since we can fit every frame type into 16 bytes (plus 4 bytes for the
        // frame type), use that as the common size. For smaller frames, the
        // remaining bytes are zero padding. The whole frame is assembled in a
        // scratch buffer so it can be written to the output in a single call.
        var buffer = _scratchBuffer.AsSpan(0, 20);
        buffer.Clear();

        // TODO: Change this to write as a short, saving 2 bytes per frame
        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)frame.FrameType);
        var data = buffer.Slice(4);

        switch (frame.FrameType)
        {
            case RenderTreeFrameType.Attribute:
                BinaryPrimitives.WriteInt32LittleEndian(data, GetStringIndex(frame.AttributeName, allowDeduplication: true));
                if (frame.AttributeValue is bool boolValue)
                {
                    // Encoding the bool as either "" or null is pretty odd, but avoids
                    // having to pack any "what type of thing is this" info into the same
                    // 4 bytes as the string table index. If, later, we need a way of
                    // distinguishing whether an attribute value is really a bool or a string
                    // or something else, we'll need a different encoding mechanism. Since there
                    // would never be more than (say) 2^28 (268 million) distinct string table
                    // entries, we could use the first 4 bits to encode the value type.
                    BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4), GetStringIndex(boolValue ? string.Empty : null, allowDeduplication: true));
                }
                else
                {
                    var attributeValueString = frame.AttributeValue as string;
                    BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4), GetStringIndex(attributeValueString, allowDeduplication: string.IsNullOrEmpty(attributeValueString)));
                }
                BinaryPrimitives.WriteUInt64LittleEndian(data.Slice(8), frame.AttributeEventHandlerId); // 8 bytes
                break;
            case RenderTreeFrameType.Component:
                BinaryPrimitives.WriteInt32LittleEndian(data, frame.ComponentSubtreeLength);
                BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4), frame.ComponentId);
                break;
            case RenderTreeFrameType.ComponentReferenceCapture:
            case RenderTreeFrameType.ComponentRenderMode:
            case RenderTreeFrameType.NamedEvent:
                // The client doesn't need to know about these. But we still have
                // to include them in the array otherwise the ReferenceFrameIndex
                // values in the edits data would be wrong.
                break;
            case RenderTreeFrameType.Element:
                BinaryPrimitives.WriteInt32LittleEndian(data, frame.ElementSubtreeLength);
                BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4), GetStringIndex(frame.ElementName, allowDeduplication: true));
                break;
            case RenderTreeFrameType.ElementReferenceCapture:
                BinaryPrimitives.WriteInt32LittleEndian(data, GetStringIndex(frame.ElementReferenceCaptureId, allowDeduplication: false));
                break;
            case RenderTreeFrameType.Region:
                BinaryPrimitives.WriteInt32LittleEndian(data, frame.RegionSubtreeLength);
                break;
            case RenderTreeFrameType.Text:
                BinaryPrimitives.WriteInt32LittleEndian(data, GetStringIndex(
                    frame.TextContent,
                    allowDeduplication: string.IsNullOrWhiteSpace(frame.TextContent)));
                break;
            case RenderTreeFrameType.Markup:
                BinaryPrimitives.WriteInt32LittleEndian(data, GetStringIndex(frame.MarkupContent, allowDeduplication: false));
                break;
            default:
                throw new ArgumentException($"Unsupported frame type: {frame.FrameType}");
        }

        _binaryWriter.Write(buffer);
    }

    int Write(in ArrayRange<int> numbers)
    {
        var startPos = (int)_binaryWriter.BaseStream.Position;
        _binaryWriter.Write(numbers.Count);

        WriteInt32Values(numbers.Array.AsSpan(0, numbers.Count));

        return startPos;
    }

    int Write(in ArrayRange<ulong> numbers)
    {
        var startPos = (int)_binaryWriter.BaseStream.Position;
        _binaryWriter.Write(numbers.Count);

        WriteUInt64Values(numbers.Array.AsSpan(0, numbers.Count));

        return startPos;
    }

    void WriteInt32Values(ReadOnlySpan<int> values)
    {
        // Encode in chunks through the scratch buffer so each chunk is a single write to the output.
        var buffer = _scratchBuffer.AsSpan(0, ScratchBufferSize);
        while (!values.IsEmpty)
        {
            var chunkLength = Math.Min(values.Length, ScratchBufferSize / sizeof(int));
            for (var i = 0; i < chunkLength; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(i * sizeof(int)), values[i]);
            }

            _binaryWriter.Write(buffer.Slice(0, chunkLength * sizeof(int)));
            values = values.Slice(chunkLength);
        }
    }

    void WriteUInt64Values(ReadOnlySpan<ulong> values)
    {
        var buffer = _scratchBuffer.AsSpan(0, ScratchBufferSize);
        while (!values.IsEmpty)
        {
            var chunkLength = Math.Min(values.Length, ScratchBufferSize / sizeof(ulong));
            for (var i = 0; i < chunkLength; i++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(i * sizeof(ulong)), values[i]);
            }

            _binaryWriter.Write(buffer.Slice(0, chunkLength * sizeof(ulong)));
            values = values.Slice(chunkLength);
        }
    }

    int GetStringIndex(string? value, bool allowDeduplication)
    {
        if (value == null)
        {
            return -1;
        }

        int stringIndex;
        if (!allowDeduplication || !_deduplicatedStringIndices.TryGetValue(value, out stringIndex))
        {
            stringIndex = _strings.Count;
            _strings.Append(value);

            if (allowDeduplication)
            {
                _deduplicatedStringIndices.Add(value, stringIndex);
            }
        }

        return stringIndex;
    }

    int WriteStringTable()
    {
        // Capture the locations of each string
        var stringsCount = _strings.Count;
        var locations = new int[stringsCount];

        for (var i = 0; i < stringsCount; i++)
        {
            var stringValue = _strings.Buffer[i];
            locations[i] = (int)_binaryWriter.BaseStream.Position;
            _binaryWriter.Write(stringValue);
        }

        // Now write the locations
        var locationsStartPos = (int)_binaryWriter.BaseStream.Position;
        for (var i = 0; i < stringsCount; i++)
        {
            _binaryWriter.Write(locations[i]);
        }

        return locationsStartPos;
    }

    int WriteStringTableUtf16()
    {
        // UTF-16LE string table: each entry is [int32 charCount][UTF-16LE chars].
        // This avoids UTF-8 encode on C# side and UTF-8 decode on JS side,
        // since both C# and JS strings are natively UTF-16.
        var stringsCount = _strings.Count;
        var locations = new int[stringsCount];

        for (var i = 0; i < stringsCount; i++)
        {
            var stringValue = _strings.Buffer[i];
            locations[i] = (int)_binaryWriter.BaseStream.Position;

            // Write char count (not byte count) as int32
            _binaryWriter.Write(stringValue.Length);

            // Write UTF-16LE chars — zero-cost reinterpret via MemoryMarshal
            var charSpan = stringValue.AsSpan();
            var byteSpan = MemoryMarshal.AsBytes(charSpan);
            _binaryWriter.BaseStream.Write(byteSpan);
        }

        // Now write the locations
        var locationsStartPos = (int)_binaryWriter.BaseStream.Position;
        for (var i = 0; i < stringsCount; i++)
        {
            _binaryWriter.Write(locations[i]);
        }

        return locationsStartPos;
    }

    public void Dispose()
    {
        _strings.Dispose();
        _binaryWriter.Dispose();

        if (_scratchBuffer is { } scratchBuffer)
        {
            _scratchBuffer = null;
            ArrayPool<byte>.Shared.Return(scratchBuffer);
        }
    }
}
