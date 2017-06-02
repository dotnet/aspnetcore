// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary.Base64;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    internal static class MessageFormatUtils
    {
        public static byte[] DecodePayload(byte[] inputPayload)
        {
            if (inputPayload.Length > 0)
            {
                // Determine the output size
                var decodedLength = Base64Decoder.ComputeDecodedLength(inputPayload);

                // Allocate a new buffer to decode to
                var decodeBuffer = new byte[decodedLength];
                if (Base64.Decoder.Transform(inputPayload, decodeBuffer, out _, out var _) != TransformationStatus.Done)
                {
                    throw new FormatException("Invalid Base64 payload");
                }
                return decodeBuffer;
            }

            return inputPayload;
        }
    }
}
