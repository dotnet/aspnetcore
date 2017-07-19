// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class Base64Encoder : IDataEncoder
    {
        public byte[] Decode(byte[] payload)
        {
            return Convert.FromBase64String(Encoding.UTF8.GetString(payload));
        }

        public byte[] Encode(byte[] payload)
        {
            return Encoding.UTF8.GetBytes(Convert.ToBase64String(payload));
        }
    }
}
