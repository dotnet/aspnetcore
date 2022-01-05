// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Http.Connections;

internal static class ServerSentEventsMessageFormatter
{
    private static readonly ReadOnlyMemory<byte> DataPrefix = new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
    private static readonly ReadOnlyMemory<byte> Newline = new[] { (byte)'\r', (byte)'\n' };

    private const byte LineFeed = (byte)'\n';

    public static async Task WriteMessageAsync(ReadOnlySequence<byte> payload, Stream output, CancellationToken token)
    {
        // Payload does not contain a line feed so write it directly to output
        if (payload.PositionOf(LineFeed) == null)
        {
            if (payload.Length > 0)
            {
                await output.WriteAsync(DataPrefix, token);
                await output.WriteAsync(payload, token);
                await output.WriteAsync(Newline, token);
            }

            await output.WriteAsync(Newline, token);
            return;
        }

        var ms = new MemoryStream();

        // Parse payload and write formatted output to memory
        await WriteMessageToMemory(ms, payload);
        ms.Position = 0;

        await ms.CopyToAsync(output, token);
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

    private static async Task WriteMessageToMemory(Stream output, ReadOnlySequence<byte> payload)
    {
        var keepWriting = true;
        while (keepWriting)
        {
            var sliceEnd = payload.PositionOf(LineFeed);

            ReadOnlySequence<byte> lineSegment;
            if (sliceEnd == null)
            {
                lineSegment = payload;
                payload = ReadOnlySequence<byte>.Empty;
                keepWriting = false;
            }
            else
            {
                lineSegment = payload.Slice(payload.Start, sliceEnd.Value);

                if (lineSegment.Length > 1)
                {
                    // Check if the line ended in \r\n. If it did then trim the \r
                    var memory = GetLastSegment(lineSegment, out var offset);
                    if (memory.Span[memory.Length - 1] == '\r')
                    {
                        lineSegment = lineSegment.Slice(lineSegment.Start, offset + memory.Length - 1);
                    }
                }

                // Update payload to remove \n
                payload = payload.Slice(payload.GetPosition(1, sliceEnd.Value));
            }

            // Write line
            await output.WriteAsync(DataPrefix);
            await output.WriteAsync(lineSegment);
            await output.WriteAsync(Newline);
        }

        await output.WriteAsync(Newline);
    }
}
