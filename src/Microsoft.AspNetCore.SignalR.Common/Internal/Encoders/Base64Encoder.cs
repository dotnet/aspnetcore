// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class Base64Encoder : IDataEncoder
    {
        public byte[] Decode(byte[] payload)
        {
            var buffer = new ReadOnlyBuffer<byte>(payload);
            TextMessageParser.TryParseMessage(ref buffer, out var message);

            return Convert.FromBase64String(Encoding.UTF8.GetString(message.ToArray()));
        }

        public byte[] Encode(byte[] payload)
        {
            var buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(payload));
            using (var stream = new MemoryStream())
            {
                TextMessageFormatter.WriteMessage(buffer, stream);
                return stream.ToArray();
            }
        }
    }
}
