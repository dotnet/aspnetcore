// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2DataFrameFlags DataFlags
        {
            get => (Http2DataFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public bool DataHasPadding => (DataFlags & Http2DataFrameFlags.PADDED) == Http2DataFrameFlags.PADDED;

        public byte DataPadLength
        {
            get => DataHasPadding ? _data[PayloadOffset] : (byte)0;
            set => _data[PayloadOffset] = value;
        }

        public ArraySegment<byte> DataPayload => DataHasPadding
            ? new ArraySegment<byte>(_data, PayloadOffset + 1, Length - DataPadLength - 1)
            : new ArraySegment<byte>(_data, PayloadOffset, Length);

        public void PrepareData(int streamId, byte? padLength = null)
        {
            var padded = padLength != null;

            Length = MinAllowedMaxFrameSize;
            Type = Http2FrameType.DATA;
            DataFlags = padded ? Http2DataFrameFlags.PADDED : Http2DataFrameFlags.NONE;
            StreamId = streamId;

            if (padded)
            {
                DataPadLength = padLength.Value;
                Payload.Slice(Length - padLength.Value).Fill(0);
            }
        }

        private void DataTraceFrame(ILogger logger)
        {
            logger.LogTrace("'DATA' Frame. Flags = {DataFlags}, PadLength = {PadLength}, PayloadLength = {PayloadLength}", DataFlags, DataPadLength, DataPayload.Count);
        }

        internal object ShowFlags()
        {
            switch (Type)
            {
                case Http2FrameType.CONTINUATION:
                    return ContinuationFlags;
                case Http2FrameType.DATA:
                    return DataFlags;
                case Http2FrameType.HEADERS:
                    return HeadersFlags;
                case Http2FrameType.SETTINGS:
                    return SettingsFlags;
                case Http2FrameType.PING:
                    return PingFlags;

                // Not Implemented
                case Http2FrameType.PUSH_PROMISE:

                // No flags defined
                case Http2FrameType.PRIORITY:
                case Http2FrameType.RST_STREAM:
                case Http2FrameType.GOAWAY:
                case Http2FrameType.WINDOW_UPDATE:
                default:
                    return $"0x{Flags:x}";
            }
        }
    }
}
