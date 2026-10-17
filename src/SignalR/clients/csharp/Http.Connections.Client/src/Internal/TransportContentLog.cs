// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal static partial class TransportContentLog
{
    internal const int MaxLoggedBytes = 1024;
    private const string HexDigits = "0123456789ABCDEF";

    public static void Write(ILogger logger, bool enabled, bool received, ReadOnlySpan<byte> data)
    {
        if (!enabled || !logger.IsEnabled(LogLevel.Trace) || data.IsEmpty)
        {
            return;
        }

        var count = Math.Min(data.Length, MaxLoggedBytes);
        var content = new StringBuilder(count);
        Append(content, data.Slice(0, count));
        Write(logger, received, data.Length, count, content.ToString());
    }

    public static void Write(ILogger logger, bool enabled, bool received, in ReadOnlySequence<byte> data)
    {
        if (!enabled || !logger.IsEnabled(LogLevel.Trace) || data.IsEmpty)
        {
            return;
        }

        var count = (int)Math.Min(data.Length, MaxLoggedBytes);
        var content = new StringBuilder(count);
        foreach (var segment in data.Slice(0, count))
        {
            if (!segment.IsEmpty)
            {
                Append(content, segment.Span);
            }
        }
        Write(logger, received, data.Length, count, content.ToString());
    }

    private static void Append(StringBuilder content, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            if (value == (byte)'\\')
            {
                content.Append("\\\\");
            }
            else if (value >= 0x20 && value <= 0x7E)
            {
                content.Append((char)value);
            }
            else
            {
                content.Append("\\x").Append(HexDigits[value >> 4]).Append(HexDigits[value & 0xF]);
            }
        }
    }

    private static void Write(ILogger logger, bool received, long count, int loggedCount, string content)
    {
        // The logging state owns a string, never a reference to transport-owned memory.
        if (received)
        {
            ReceivedContent(logger, count, loggedCount, count > loggedCount, content);
        }
        else
        {
            SendingContent(logger, count, loggedCount, count > loggedCount, content);
        }
    }

    // Shared by all transports; IDs must not overlap their logs or SendUtils (100-106).
    [LoggerMessage(200, LogLevel.Trace, "Received data chunk. Length: {ByteCount} bytes. Logged: {LoggedByteCount} bytes. Truncated: {Truncated}. Content: {Content}", EventName = "ReceivedMessageContent", SkipEnabledCheck = true)]
    private static partial void ReceivedContent(ILogger logger, long byteCount, int loggedByteCount, bool truncated, string content);

    [LoggerMessage(201, LogLevel.Trace, "Sending data chunk. Length: {ByteCount} bytes. Logged: {LoggedByteCount} bytes. Truncated: {Truncated}. Content: {Content}", EventName = "SendingMessageContent", SkipEnabledCheck = true)]
    private static partial void SendingContent(ILogger logger, long byteCount, int loggedByteCount, bool truncated, string content);
}
