// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class Base64Encoder : IDataEncoder
    {
        public ReadOnlySpan<byte> Decode(byte[] payload)
        {
            ReadOnlySpan<byte> buffer = payload;
            LengthPrefixedTextMessageParser.TryParseMessage(ref buffer, out var message);

            Span<byte> decoded = new byte[Base64.GetMaxDecodedFromUtf8Length(message.Length)];
            var status = Base64.DecodeFromUtf8(message, decoded, out _, out var written);
            Debug.Assert(status == OperationStatus.Done);

            return decoded.Slice(0, written);
        }

        public byte[] Encode(byte[] payload)
        {
            Span<byte> buffer = new byte[Base64.GetMaxEncodedToUtf8Length(payload.Length)];

            var status = Base64.EncodeToUtf8(payload, buffer, out _, out var written);
            Debug.Assert(status == OperationStatus.Done);

            using (var stream = new MemoryStream())
            {
                LengthPrefixedTextMessageWriter.WriteMessage(buffer.Slice(0, written), stream);
                return stream.ToArray();
            }
        }
    }
}
