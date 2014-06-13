// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;

namespace Microsoft.AspNet.WebSockets.Protocol
{
    public static class Utilities
    {
        // Copies the header and data into a new buffer and masks the data.
        public static byte[] MergeAndMask(int mask, ArraySegment<byte> header, ArraySegment<byte> data)
        {
            byte[] frame = new byte[header.Count + data.Count];
            Array.Copy(header.Array, header.Offset, frame, 0, header.Count);
            Array.Copy(data.Array, data.Offset, frame, header.Count, data.Count);

            MaskInPlace(mask, new ArraySegment<byte>(frame, header.Count, data.Count));
            return frame;
        }

        public static void MaskInPlace(int mask, ArraySegment<byte> data)
        {
            int maskOffset = 0;
            MaskInPlace(mask, ref maskOffset, data);
        }

        public static void MaskInPlace(int mask, ref int maskOffset, ArraySegment<byte> data)
        {
            if (mask == 0)
            {
                return;
            }

            byte[] maskBytes = new byte[]
            {
                (byte)(mask >> 24),
                (byte)(mask >> 16),
                (byte)(mask >> 8),
                (byte)mask,
            };

            int end = data.Offset + data.Count;
            for (int i = data.Offset; i < end; i++)
            {
                data.Array[i] ^= maskBytes[maskOffset];
                maskOffset = (maskOffset + 1) & 0x3; // fast % 4;
            }
        }

        public static int GetOpCode(WebSocketMessageType messageType)
        {
            switch (messageType)
            {
                case WebSocketMessageType.Text: return Constants.OpCodes.TextFrame;
                case WebSocketMessageType.Binary: return Constants.OpCodes.BinaryFrame;
                case WebSocketMessageType.Close: return Constants.OpCodes.CloseFrame;
                default: throw new NotImplementedException(messageType.ToString());
            }
        }

        public static WebSocketMessageType GetMessageType(int opCode)
        {
            switch (opCode)
            {
                case Constants.OpCodes.TextFrame: return WebSocketMessageType.Text;
                case Constants.OpCodes.BinaryFrame: return WebSocketMessageType.Binary;
                case Constants.OpCodes.CloseFrame: return WebSocketMessageType.Close;
                default: throw new NotImplementedException(opCode.ToString());
            }
        }
    }
}
