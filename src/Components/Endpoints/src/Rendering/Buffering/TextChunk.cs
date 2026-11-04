// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// Holds different types of outputs that can be written to BufferedTextWriter
internal readonly struct TextChunk
{
    private readonly TextChunkType _type;

    // If expanding this struct to hold many other possible value types,
    // consider making it into a [StructLayout(Layout.Explicit)] so different
    // value/reference types can share the same memory slots, discriminated
    // by _type. That will reduce memory usage and improve locality.
    private readonly string? _stringValue;
    private readonly char _charValue;
    private readonly int _charArraySegmentStart;
    private readonly int _charArraySegmentLength;
    private readonly int _intValue;

    public TextChunk(string value)
    {
        _type = TextChunkType.String;
        _stringValue = value;
    }

    public TextChunk(char value)
    {
        _type = TextChunkType.Char;
        _charValue = value;
    }

    public TextChunk(ArraySegment<char> value, StringBuilder charArraySegmentScope)
    {
        // An ArraySegment<char> is mutable (as in, its underlying buffer is). So
        // we must copy its value. To avoid this being a separate allocation each time,
        // use a StringBuilder as a growable buffer for these values. We rely on
        // the caller of WriteToAsync being able to supply the .ToString() result
        // of that StringBuilder, since we don't want to call that on each WriteToAsync.
        _type = TextChunkType.CharArraySegment;
        _charArraySegmentStart = charArraySegmentScope.Length;
        _charArraySegmentLength = value.Count;
        charArraySegmentScope.Append((Span<char>)value);
    }

    public TextChunk(int value)
    {
        _type = TextChunkType.Int;
        _intValue = value;
    }

    public Task WriteToAsync(TextWriter writer, string charArraySegments, ref StringBuilder? tempBuffer)
    {
        switch (_type)
        {
            case TextChunkType.String:
                return writer.WriteAsync(_stringValue);
            case TextChunkType.Char:
                return writer.WriteAsync(_charValue);
            case TextChunkType.CharArraySegment:
                return writer.WriteAsync(charArraySegments.AsMemory(_charArraySegmentStart, _charArraySegmentLength));
            case TextChunkType.Int:
                // The same technique could be used to optimize writing other
                // nonstring types, but currently only int is often used
                tempBuffer ??= new();
                tempBuffer.Clear();
                tempBuffer.Append(_intValue);
                return writer.WriteAsync(tempBuffer);
            default:
                throw new InvalidOperationException($"Unknown type {_type}");
        }
    }

    private enum TextChunkType { Int, String, Char, CharArraySegment };
}
