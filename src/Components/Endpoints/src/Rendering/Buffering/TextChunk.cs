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
    private readonly int _charArraySegmentPage;
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

    private TextChunk(int charArraySegmentPage, int charArraySegmentStart, int charArraySegmentLength)
    {
        _type = TextChunkType.CharArraySegment;
        _charArraySegmentPage = charArraySegmentPage;
        _charArraySegmentStart = charArraySegmentStart;
        _charArraySegmentLength = charArraySegmentLength;
    }

    // Represents a range within one of the char pages that are supplied to WriteToAsync. This avoids
    // storing a reference to caller-supplied char arrays, which may be mutated after the write.
    public static TextChunk ForCharArraySegment(int page, int start, int length)
        => new TextChunk(page, start, length);

    public TextChunk(int value)
    {
        _type = TextChunkType.Int;
        _intValue = value;
    }

    public Task WriteToAsync(TextWriter writer, List<char[]> charPages, ref StringBuilder? tempBuffer)
    {
        switch (_type)
        {
            case TextChunkType.String:
                return writer.WriteAsync(_stringValue);
            case TextChunkType.Char:
                return writer.WriteAsync(_charValue);
            case TextChunkType.CharArraySegment:
                return writer.WriteAsync(charPages[_charArraySegmentPage].AsMemory(_charArraySegmentStart, _charArraySegmentLength));
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
