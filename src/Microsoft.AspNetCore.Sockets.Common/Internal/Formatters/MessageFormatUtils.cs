// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    internal static class MessageFormatUtils
    {
        public static byte[] DecodePayload(byte[] inputPayload)
        {
            if (inputPayload.Length > 0)
            {
                // Determine the output size
                // Every 4 Base64 characters represents 3 bytes
                var decodedLength = (inputPayload.Length / 4) * 3;

                // Subtract padding bytes
                if (inputPayload[inputPayload.Length - 1] == '=')
                {
                    decodedLength -= 1;
                }
                if (inputPayload.Length > 1 && inputPayload[inputPayload.Length - 2] == '=')
                {
                    decodedLength -= 1;
                }

                // Allocate a new buffer to decode to
                var decodeBuffer = new byte[decodedLength];
                if (Base64.Decode(inputPayload, decodeBuffer) != decodedLength)
                {
                    throw new FormatException("Invalid Base64 payload");
                }
                return decodeBuffer;
            }

            return inputPayload;
        }
    }
}
