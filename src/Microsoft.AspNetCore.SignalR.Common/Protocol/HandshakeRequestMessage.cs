// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public class HandshakeRequestMessage : HubMessage
    {
        public HandshakeRequestMessage(string protocol, int version)
        {
            Protocol = protocol;
            Version = version;
        }

        public string Protocol { get; }
        public int Version { get; }
    }
}
