// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebSockets.Protocol
{
    public class FrameHeader
    {
        private byte[] _header;

        public FrameHeader(ArraySegment<byte> header)
        {
            _header = new byte[header.Count];
            Array.Copy(header.Array, header.Offset, _header, 0, _header.Length);
        }

        public FrameHeader(bool final, int opCode, bool masked, int maskKey, long dataLength)
        {
            int headerLength = 2;
            if (masked)
            {
                headerLength += 4;
            }

            if (dataLength <= 125)
            {
            }
            else if (125 < dataLength && dataLength <= 0xFFFF)
            {
                headerLength += 2;
            }
            else
            {
                headerLength += 8;
            }
            _header = new byte[headerLength];

            Fin = final;
            OpCode = opCode;
            Masked = masked;
            DataLength = dataLength;
            if (masked)
            {
                MaskKey = maskKey;
            }
        }

        public bool Fin
        {
            get
            {
                return (_header[0] & 0x80) == 0x80;
            }
            private set
            {
                if (value)
                {
                    _header[0] |= 0x80;
                }
                else
                {
                    _header[0] &= 0x7F;
                }
            }
        }

        public int OpCode
        {
            get
            {
                return (_header[0] & 0xF);
            }
            private set
            {
                // TODO: Clear out a prior value?
                _header[0] |= (byte)(value & 0xF);
            }
        }

        public bool Masked
        {
            get
            {
                return (_header[1] & 0x80) == 0x80;
            }
            private set
            {
                if (value)
                {
                    _header[1] |= 0x80;
                }
                else
                {
                    _header[1] &= 0x7F;
                }
            }
        }

        public int MaskKey
        {
            get
            {
                if (!Masked)
                {
                    return 0;
                }
                int offset = ExtendedLengthFieldSize + 2;
                return (_header[offset] << 24) + (_header[offset + 1] << 16)
                    + (_header[offset + 2] << 8) + _header[offset + 3];
            }
            private set
            {
                int offset = ExtendedLengthFieldSize + 2;
                _header[offset] = (byte)(value >> 24);
                _header[offset + 1] = (byte)(value >> 16);
                _header[offset + 2] = (byte)(value >> 8);
                _header[offset + 3] = (byte)value;
            }
        }

        public int PayloadField
        {
            get
            {
                return (_header[1] & 0x7F);
            }
            private set
            {
                // TODO: Clear out a prior value?
                _header[1] |= (byte)(value & 0x7F);
            }
        }

        public int ExtendedLengthFieldSize
        {
            get
            {
                int payloadField = PayloadField;
                if (payloadField <= 125)
                {
                    return 0;
                }
                if (payloadField == 126)
                {
                    return 2;
                }
                return 8;
            }
        }

        public long DataLength
        {
            get
            {
                int extendedFieldSize = ExtendedLengthFieldSize;
                if (extendedFieldSize == 0)
                {
                    return PayloadField;
                }
                if (extendedFieldSize == 2)
                {
                    return (_header[2] << 8) + _header[3];
                }
                return (_header[2] << 56) + (_header[3] << 48)
                    + (_header[4] << 40) + (_header[5] << 32)
                    + (_header[6] << 24) + (_header[7] << 16)
                    + (_header[8] << 8) + _header[9];
            }
            private set
            {
                if (value <= 125)
                {
                    PayloadField = (int)value;
                }
                else if (125 < value && value <= 0xFFFF)
                {
                    PayloadField = 0x7E;

                    _header[2] = (byte)(value >> 8);
                    _header[3] = (byte)value;
                }
                else
                {
                    PayloadField = 0x7F;

                    _header[2] = (byte)(value >> 56);
                    _header[3] = (byte)(value >> 48);
                    _header[4] = (byte)(value >> 40);
                    _header[5] = (byte)(value >> 32);
                    _header[6] = (byte)(value >> 24);
                    _header[7] = (byte)(value >> 16);
                    _header[8] = (byte)(value >> 8);
                    _header[9] = (byte)value;
                }
            }
        }

        public ArraySegment<byte> Buffer
        {
            get
            {
                return new ArraySegment<byte>(_header);
            }
        }

        public bool IsControlFrame
        {
            get
            {
                return OpCode >= Constants.OpCodes.CloseFrame;
            }
        }

        // bits 1-3.
        internal bool AreReservedSet()
        {
            return (_header[0] & 0x70) != 0;
        }

        // Given the second bytes of a frame, calculate how long the whole frame header should be.
        // Range 2-12 bytes
        public static int CalculateFrameHeaderSize(byte b2)
        {
            int headerLength = 2;
            if ((b2 & 0x80) == 0x80) // Masked
            {
                headerLength += 4;
            }

            int payloadField = (b2 & 0x7F);
            if (payloadField <= 125)
            {
                // headerLength += 0
            }
            else if (payloadField == 126)
            {
                headerLength += 2;
            }
            else
            {
                headerLength += 8;
            }
            return headerLength;
        }
    }
}
