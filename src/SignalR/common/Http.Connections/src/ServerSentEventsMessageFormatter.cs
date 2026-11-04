// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http.Connections;

internal static class ServerSentEventsMessageFormatter
{
    private static readonly ReadOnlyMemory<byte> DataPrefix = new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
    private static readonly ReadOnlyMemory<byte> Newline = new[] { (byte)'\r', (byte)'\n' };

    private const byte LineFeed = (byte)'\n';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task WriteMessageAsync(ReadOnlySequence<byte> payload, Stream output, CancellationToken token)
    {
        // Payload does not contain a line feed so write it directly to output
        return payload.PositionOf(LineFeed) is null
            ? WriteSingleLineAsync(payload, output, token)
            : WriteMultilineAsync(payload, output, token);
    }

    private static async Task WriteSingleLineAsync(ReadOnlySequence<byte> payload, Stream output, CancellationToken token)
    {
        if (payload.Length > 0)
        {
            await output.WriteAsync(DataPrefix, token);
            await output.WriteAsync(payload, token);
            await output.WriteAsync(Newline, token);
        }

        await output.WriteAsync(Newline, token);
    }

    private static async Task WriteMultilineAsync(ReadOnlySequence<byte> payload, Stream output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var maximumLength = GetFormattedLengthUpperBound(payload);
        var buffer = ArrayPool<byte>.Shared.Rent(maximumLength);
        try
        {
            // Keep the output coalesced, but do not write unused space from trimmed carriage returns.
            var length = WriteMessageToMemory(buffer.AsSpan(0, maximumLength), payload);
            token.ThrowIfCancellationRequested();
            await output.WriteAsync(buffer.AsMemory(0, length), token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int GetFormattedLengthUpperBound(ReadOnlySequence<byte> payload)
    {
        var length = checked(payload.Length + DataPrefix.Length + (2 * Newline.Length));
        foreach (var segment in payload)
        {
            // Each LF is replaced by CRLF and a data prefix. Trimming CR can only reduce the length.
            length = checked(length + ((long)segment.Span.Count(LineFeed) * (DataPrefix.Length + Newline.Length - 1)));
        }

        return checked((int)length);
    }

    /// <summary>
    /// Gets the last memory segment in a sequence.
    /// </summary>
    /// <param name="source">Source sequence.</param>
    /// <param name="offset">The offset the segment starts at.</param>
    /// <returns>The last memory segment in a sequence.</returns>
    private static ReadOnlyMemory<byte> GetLastSegment(in ReadOnlySequence<byte> source, out long offset)
    {
        offset = 0;

        var totalLength = source.Length;
        var position = source.Start;
        while (source.TryGet(ref position, out ReadOnlyMemory<byte> memory))
        {
            // Last segment
            if (offset + memory.Length >= totalLength)
            {
                return memory;
            }

            offset += memory.Length;
        }

        throw new InvalidOperationException("Could not get last segment from sequence.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySequence<byte> ReadLine(ref ReadOnlySequence<byte> payload, out bool hasMore)
    {
        var sliceEnd = payload.PositionOf(LineFeed);
        if (sliceEnd is null)
        {
            var remaining = payload;
            payload = ReadOnlySequence<byte>.Empty;
            hasMore = false;
            return remaining;
        }

        var line = payload.Slice(payload.Start, sliceEnd.Value);
        if (line.Length > 1)
        {
            // Preserve the existing CRLF trimming behavior, including across segment boundaries.
            var memory = GetLastSegment(line, out var offset);
            if (memory.Span[memory.Length - 1] == '\r')
            {
                line = line.Slice(line.Start, offset + memory.Length - 1);
            }
        }

        payload = payload.Slice(payload.GetPosition(1, sliceEnd.Value));
        hasMore = true;
        return line;
    }

    private static int WriteMessageToMemory(Span<byte> output, ReadOnlySequence<byte> payload)
    {
        var initialLength = output.Length;
        bool hasMore;
        do
        {
            var line = ReadLine(ref payload, out hasMore);
            DataPrefix.Span.CopyTo(output);
            output = output.Slice(DataPrefix.Length);
            line.CopyTo(output);
            output = output.Slice((int)line.Length);
            Newline.Span.CopyTo(output);
            output = output.Slice(Newline.Length);
        }
        while (hasMore);

        Newline.Span.CopyTo(output);

        return initialLength - output.Length + Newline.Length;
    }
}
