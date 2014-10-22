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

        // For now this is stateless and does not handle sequences spliced across messages.
        // http://etutorials.org/Programming/secure+programming/Chapter+3.+Input+Validation/3.12+Detecting+Illegal+UTF-8+Characters/
        public static bool TryValidateUtf8(ArraySegment<byte> arraySegment, bool endOfMessage, Utf8MessageState state)
        {
            for (int i = arraySegment.Offset; i < arraySegment.Offset + arraySegment.Count; )
            {
                if (!state.SequenceInProgress)
                {
                    state.SequenceInProgress = true;
                    byte b = arraySegment.Array[i];
                    if ((b & 0x80) == 0) // 0bbbbbbb, single byte
                    {
                        state.AdditionalBytesExpected = 0;
                    }
                    else if ((b & 0xC0) == 0x80)
                    {
                        return false; // Misplaced 10bbbbbb byte. This cannot be the first byte.
                    }
                    else if ((b & 0xE0) == 0xC0) // 110bbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 1;
                    }
                    else if ((b & 0xF0) == 0xE0) // 1110bbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 2;
                    }
                    else if ((b & 0xF8) == 0xF0) // 11110bbb 10bbbbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 3;
                    }
                    else if ((b & 0xFC) == 0xF8) // 111110bb 10bbbbbb 10bbbbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 4;
                    }
                    else if ((b & 0xFE) == 0xFC) // 1111110b 10bbbbbb 10bbbbbb 10bbbbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 5;
                    }
                    else // 11111110 && 11111111 are not valid
                    {
                        return false;
                    }
                    i++;
                }
                while (state.AdditionalBytesExpected > 0 && i < arraySegment.Offset + arraySegment.Count)
                {
                    byte b = arraySegment.Array[i];
                    if ((b & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    state.AdditionalBytesExpected--;
                    i++;
                }
                if (state.AdditionalBytesExpected == 0)
                {
                    state.SequenceInProgress = false;
                }
            }
            if (endOfMessage && state.SequenceInProgress)
            {
                return false;
            }
            return true;
        }

        public class Utf8MessageState
        {
            public bool SequenceInProgress { get; set; }
            public int AdditionalBytesExpected { get; set; }
        }
    }
}
