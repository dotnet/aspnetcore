// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Http2HeadersEnumerator = Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2HeadersEnumerator;
using HPackHeaderWriter = Microsoft.AspNetCore.Server.Kestrel.Core.Tests.HPackHeaderWriter;
using HeaderWriteResult = Microsoft.AspNetCore.Server.Kestrel.Core.Tests.HeaderWriteResult;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class PipeWriterHttp2FrameExtensions
{
    public static void WriteSettings(this PipeWriter writer, Http2PeerSettings clientSettings)
    {
        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
        var settings = clientSettings.GetNonProtocolDefaults();
        var payload = new byte[settings.Count * Http2FrameReader.SettingSize];
        frame.PayloadLength = payload.Length;
        Http2FrameWriter.WriteSettings(settings, payload);
        Http2FrameWriter.WriteHeader(frame, writer);
        writer.Write(payload);
    }

    public static void WriteStartStream(this PipeWriter writer, int streamId, DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headers, byte[] headerEncodingBuffer, bool endStream, Http2Frame frame = null)
    {
        frame ??= new Http2Frame();
        frame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);

        var buffer = headerEncodingBuffer.AsSpan();
        var done = HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, headers, buffer, out var length);
        frame.PayloadLength = length;

        if (done == HeaderWriteResult.Done)
        {
            frame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
        }

        if (endStream)
        {
            frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, writer);
        writer.Write(buffer.Slice(0, length));

        while (done != HeaderWriteResult.Done)
        {
            frame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);

            done = HPackHeaderWriter.ContinueEncodeHeaders(hpackEncoder, headers, buffer, out length);
            frame.PayloadLength = length;

            if (done == HeaderWriteResult.Done)
            {
                frame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
            }

            Http2FrameWriter.WriteHeader(frame, writer);
            writer.Write(buffer.Slice(0, length));
        }
    }

    public static void WriteStartStream(this PipeWriter writer, int streamId, Span<byte> headerData, bool endStream, Http2Frame frame = null)
    {
        frame ??= new Http2Frame();
        frame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);
        frame.PayloadLength = headerData.Length;
        frame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;

        if (endStream)
        {
            frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, writer);
        writer.Write(headerData);
    }

    public static void WriteData(this PipeWriter writer, int streamId, Memory<byte> data, bool endStream, Http2Frame frame = null)
    {
        frame ??= new Http2Frame();
        frame.PrepareData(streamId);
        frame.PayloadLength = data.Length;
        frame.DataFlags = endStream ? Http2DataFrameFlags.END_STREAM : Http2DataFrameFlags.NONE;

        Http2FrameWriter.WriteHeader(frame, writer);
        writer.Write(data.Span);
    }

    public static void WriteWindowUpdateAsync(this PipeWriter writer, int streamId, int sizeIncrement, Http2Frame frame = null)
    {
        frame ??= new Http2Frame();
        frame.PrepareWindowUpdate(streamId, sizeIncrement);
        Http2FrameWriter.WriteHeader(frame, writer);
        BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(4), (uint)sizeIncrement);
        writer.Advance(4);
    }
}
