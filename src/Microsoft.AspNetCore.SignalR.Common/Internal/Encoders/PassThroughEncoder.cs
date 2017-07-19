// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class PassThroughEncoder : IDataEncoder
    {
        public byte[] Decode(byte[] payload)
        {
            return payload;
        }

        public byte[] Encode(byte[] payload)
        {
            return payload;
        }
    }
}
