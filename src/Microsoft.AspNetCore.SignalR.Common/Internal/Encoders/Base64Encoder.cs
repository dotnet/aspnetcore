// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class Base64Encoder : IDataEncoder
    {
        public bool TryDecode(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> data)
        {
            if (LengthPrefixedTextMessageParser.TryParseMessage(ref buffer, out var message))
            {
                Span<byte> decoded = new byte[Base64.GetMaxDecodedFromUtf8Length(message.Length)];
                var status = Base64.DecodeFromUtf8(message, decoded, out _, out var written);
                Debug.Assert(status == OperationStatus.Done);
                data = decoded.Slice(0, written);
                return true;
            }
            return false;
        }

        private const int Int32OverflowLength = 10;

        public byte[] Encode(byte[] payload)
        {
            var maxEncodedLength = Base64.GetMaxEncodedToUtf8Length(payload.Length);

            // Int32OverflowLength + length of separator (':') + length of terminator (';')
            if (int.MaxValue - maxEncodedLength < Int32OverflowLength + 2)
            {
                throw new FormatException("The encoded message exceeds the maximum supported size.");
            }

            //The format is: [{length}:{message};] so allocate enough to be able to write the entire message
            Span<byte> buffer = new byte[Int32OverflowLength + 1 + maxEncodedLength + 1];

            buffer[Int32OverflowLength] = (byte)':';
            var status = Base64.EncodeToUtf8(payload, buffer.Slice(Int32OverflowLength + 1), out _, out var written);
            Debug.Assert(status == OperationStatus.Done);

            buffer[Int32OverflowLength + 1 + written] = (byte)';';
            var prefixLength = 0;
            var prefix = written;
            do
            {
                buffer[Int32OverflowLength - 1 - prefixLength] = (byte)('0' + prefix % 10);
                prefix /= 10;
                prefixLength++;
            }
            while (prefix > 0);

            return buffer.Slice(Int32OverflowLength - prefixLength, prefixLength + 1 + written + 1).ToArray();
        }
    }
}
