// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class ReceiveData
    {
        public byte[] Data { get; set; }

        public MessageType MessageType { get; set; }
    }
}
