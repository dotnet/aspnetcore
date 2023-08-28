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
    private readonly ArraySegment<char> _charArraySegmentValue;
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

    public TextChunk(ArraySegment<char> value)
    {
        _type = TextChunkType.CharArraySegment;
        _charArraySegmentValue = value;
    }

    public TextChunk(int value)
    {
        _type = TextChunkType.Int;
        _intValue = value;
    }

    public Task WriteToAsync(TextWriter writer, ref StringBuilder? tempBuffer)
    {
        switch (_type)
        {
            case TextChunkType.String:
                return writer.WriteAsync(_stringValue);
            case TextChunkType.Char:
                return writer.WriteAsync(_charValue);
            case TextChunkType.CharArraySegment:
                return writer.WriteAsync(_charArraySegmentValue);
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
