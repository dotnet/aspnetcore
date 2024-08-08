// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal static class Http2FrameReader
{
    /* https://tools.ietf.org/html/rfc7540#section-4.1
        +-----------------------------------------------+
        |                 Length (24)                   |
        +---------------+---------------+---------------+
        |   Type (8)    |   Flags (8)   |
        +-+-------------+---------------+-------------------------------+
        |R|                 Stream Identifier (31)                      |
        +=+=============================================================+
        |                   Frame Payload (0...)                      ...
        +---------------------------------------------------------------+
    */
    public const int HeaderLength = 9;

    private const int TypeOffset = 3;
    private const int FlagsOffset = 4;
    private const int StreamIdOffset = 5;

    public const int SettingSize = 6; // 2 bytes for the id, 4 bytes for the value.

    public static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, Http2Frame frame, uint maxFrameSize, out ReadOnlySequence<byte> framePayload)
    {
        framePayload = ReadOnlySequence<byte>.Empty;

        if (buffer.Length < HeaderLength)
        {
            return false;
        }

        var headerSlice = buffer.Slice(0, HeaderLength);
        var header = headerSlice.ToSpan();

        var payloadLength = (int)Bitshifter.ReadUInt24BigEndian(header);
        if (payloadLength > maxFrameSize)
        {
            throw new Http2ConnectionErrorException(SharedStrings.FormatHttp2ErrorFrameOverLimit(payloadLength, maxFrameSize), Http2ErrorCode.FRAME_SIZE_ERROR, ConnectionEndReason.MaxFrameLengthExceeded);
        }

        // Make sure the whole frame is buffered
        var frameLength = HeaderLength + payloadLength;
        if (buffer.Length < frameLength)
        {
            return false;
        }

        frame.PayloadLength = payloadLength;
        frame.Type = (Http2FrameType)header[TypeOffset];
        frame.Flags = header[FlagsOffset];
        frame.StreamId = (int)Bitshifter.ReadUInt31BigEndian(header.Slice(StreamIdOffset));

        var extendedHeaderLength = ReadExtendedFields(frame, buffer);

        // The remaining payload minus the extra fields
        framePayload = buffer.Slice(HeaderLength + extendedHeaderLength, payloadLength - extendedHeaderLength);
        buffer = buffer.Slice(framePayload.End);

        return true;
    }

    private static int ReadExtendedFields(Http2Frame frame, in ReadOnlySequence<byte> readableBuffer)
    {
        // Copy in any extra fields for the given frame type
        var extendedHeaderLength = GetPayloadFieldsLength(frame);

        if (extendedHeaderLength > frame.PayloadLength)
        {
            throw new Http2ConnectionErrorException(
                SharedStrings.FormatHttp2ErrorUnexpectedFrameLength(frame.Type, expectedLength: extendedHeaderLength), Http2ErrorCode.FRAME_SIZE_ERROR, ConnectionEndReason.InvalidFrameLength);
        }

        var extendedHeaders = readableBuffer.Slice(HeaderLength, extendedHeaderLength).ToSpan();

        // Parse frame type specific fields
        switch (frame.Type)
        {
            /*
                +---------------+
                |Pad Length? (8)|
                +---------------+-----------------------------------------------+
                |                            Data (*)                         ...
                +---------------------------------------------------------------+
                |                           Padding (*)                       ...
                +---------------------------------------------------------------+
            */
            case Http2FrameType.DATA: // Variable 0 or 1
                frame.DataPadLength = frame.DataHasPadding ? extendedHeaders[0] : (byte)0;
                break;

            /* https://tools.ietf.org/html/rfc7540#section-6.2
                +---------------+
                |Pad Length? (8)|
                +-+-------------+-----------------------------------------------+
                |E|                 Stream Dependency? (31)                     |
                +-+-------------+-----------------------------------------------+
                |  Weight? (8)  |
                +-+-------------+-----------------------------------------------+
                |                   Header Block Fragment (*)                 ...
                +---------------------------------------------------------------+
                |                           Padding (*)                       ...
                +---------------------------------------------------------------+
            */
            case Http2FrameType.HEADERS:
                if (frame.HeadersHasPadding)
                {
                    frame.HeadersPadLength = extendedHeaders[0];
                    extendedHeaders = extendedHeaders.Slice(1);
                }
                else
                {
                    frame.HeadersPadLength = 0;
                }

                if (frame.HeadersHasPriority)
                {
                    frame.HeadersStreamDependency = (int)Bitshifter.ReadUInt31BigEndian(extendedHeaders);
                    frame.HeadersPriorityWeight = extendedHeaders.Slice(4)[0];
                }
                else
                {
                    frame.HeadersStreamDependency = 0;
                    frame.HeadersPriorityWeight = 0;
                }
                break;

            /* https://tools.ietf.org/html/rfc7540#section-6.8
                +-+-------------------------------------------------------------+
                |R|                  Last-Stream-ID (31)                        |
                +-+-------------------------------------------------------------+
                |                      Error Code (32)                          |
                +---------------------------------------------------------------+
                |                  Additional Debug Data (*)                    |
                +---------------------------------------------------------------+
            */
            case Http2FrameType.GOAWAY:
                frame.GoAwayLastStreamId = (int)Bitshifter.ReadUInt31BigEndian(extendedHeaders);
                frame.GoAwayErrorCode = (Http2ErrorCode)BinaryPrimitives.ReadUInt32BigEndian(extendedHeaders.Slice(4));
                break;

            /* https://tools.ietf.org/html/rfc7540#section-6.3
                +-+-------------------------------------------------------------+
                |E|                  Stream Dependency (31)                     |
                +-+-------------+-----------------------------------------------+
                |   Weight (8)  |
                +-+-------------+
            */
            case Http2FrameType.PRIORITY:
                frame.PriorityStreamDependency = (int)Bitshifter.ReadUInt31BigEndian(extendedHeaders);
                frame.PriorityWeight = extendedHeaders.Slice(4)[0];
                break;

            /* https://tools.ietf.org/html/rfc7540#section-6.4
                +---------------------------------------------------------------+
                |                        Error Code (32)                        |
                +---------------------------------------------------------------+
            */
            case Http2FrameType.RST_STREAM:
                frame.RstStreamErrorCode = (Http2ErrorCode)BinaryPrimitives.ReadUInt32BigEndian(extendedHeaders);
                break;

            /* https://tools.ietf.org/html/rfc7540#section-6.9
                +-+-------------------------------------------------------------+
                |R|              Window Size Increment (31)                     |
                +-+-------------------------------------------------------------+
            */
            case Http2FrameType.WINDOW_UPDATE:
                frame.WindowUpdateSizeIncrement = (int)Bitshifter.ReadUInt31BigEndian(extendedHeaders);
                break;

            case Http2FrameType.PING: // Opaque payload 8 bytes long
            case Http2FrameType.SETTINGS: // Settings are general payload
            case Http2FrameType.CONTINUATION: // None
            case Http2FrameType.PUSH_PROMISE: // Not implemented frames are ignored at this phase
            default:
                return 0;
        }

        return extendedHeaderLength;
    }

    // The length in bytes of additional fields stored in the payload section.
    // This may be variable based on flags, but should be no more than 8 bytes.
    public static int GetPayloadFieldsLength(Http2Frame frame)
    {
        switch (frame.Type)
        {
            // TODO: Extract constants
            case Http2FrameType.DATA: // Variable 0 or 1
                return frame.DataHasPadding ? 1 : 0;
            case Http2FrameType.HEADERS:
                return (frame.HeadersHasPadding ? 1 : 0) + (frame.HeadersHasPriority ? 5 : 0); // Variable 0 to 6
            case Http2FrameType.GOAWAY:
                return 8; // Last stream id and error code.
            case Http2FrameType.PRIORITY:
                return 5; // Stream dependency and weight
            case Http2FrameType.RST_STREAM:
                return 4; // Error code
            case Http2FrameType.WINDOW_UPDATE:
                return 4; // Update size
            case Http2FrameType.PING: // 8 bytes of opaque data
            case Http2FrameType.SETTINGS: // Settings are general payload
            case Http2FrameType.CONTINUATION: // None
            case Http2FrameType.PUSH_PROMISE: // Not implemented frames are ignored at this phase
            default:
                return 0;
        }
    }

    public static IList<Http2PeerSetting> ReadSettings(in ReadOnlySequence<byte> payload)
    {
        var data = payload.ToSpan();
        Debug.Assert(data.Length % SettingSize == 0, "Invalid settings payload length");
        var settingsCount = data.Length / SettingSize;

        var settings = new Http2PeerSetting[settingsCount];
        for (int i = 0; i < settings.Length; i++)
        {
            settings[i] = ReadSetting(data);
            data = data.Slice(SettingSize);
        }
        return settings;
    }

    private static Http2PeerSetting ReadSetting(ReadOnlySpan<byte> payload)
    {
        var id = (Http2SettingsParameter)BinaryPrimitives.ReadUInt16BigEndian(payload);
        var value = BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(2));

        return new Http2PeerSetting(id, value);
    }
}
