// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
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
    public partial class Http2Frame
    {
        public Http2DataFrameFlags DataFlags
        {
            get => (Http2DataFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public bool DataEndStream => (DataFlags & Http2DataFrameFlags.END_STREAM) == Http2DataFrameFlags.END_STREAM;

        public bool DataHasPadding => (DataFlags & Http2DataFrameFlags.PADDED) == Http2DataFrameFlags.PADDED;

        public byte DataPadLength
        {
            get => DataHasPadding ? Payload[0] : (byte)0;
            set => Payload[0] = value;
        }

        public int DataPayloadOffset => DataHasPadding ? 1 : 0;

        private int DataPayloadLength => PayloadLength - DataPayloadOffset - DataPadLength;

        public Span<byte> DataPayload => Payload.Slice(DataPayloadOffset, DataPayloadLength);

        public void PrepareData(int streamId, byte? padLength = null)
        {
            var padded = padLength != null;

            PayloadLength = MinAllowedMaxFrameSize;
            Type = Http2FrameType.DATA;
            DataFlags = padded ? Http2DataFrameFlags.PADDED : Http2DataFrameFlags.NONE;
            StreamId = streamId;

            if (padded)
            {
                DataPadLength = padLength.Value;
                Payload.Slice(PayloadLength - padLength.Value).Fill(0);
            }
        }
    }
}
