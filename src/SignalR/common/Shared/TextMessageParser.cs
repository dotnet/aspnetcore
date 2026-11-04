// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Internal;

internal static class TextMessageParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.First.Span;
            var index = span.IndexOf(TextMessageFormatter.RecordSeparator);
            if (index == -1)
            {
                payload = default;
                return false;
            }

            payload = buffer.Slice(0, index);

            buffer = buffer.Slice(index + 1);

            return true;
        }
        else
        {
            return TryParseMessageMultiSegment(ref buffer, out payload);
        }
    }

    private static bool TryParseMessageMultiSegment(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
    {
        var position = buffer.PositionOf(TextMessageFormatter.RecordSeparator);
        if (position == null)
        {
            payload = default;
            return false;
        }

        payload = buffer.Slice(0, position.Value);

        // Skip record separator
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }
}
