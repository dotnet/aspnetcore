// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using MsgPack.Serialization;

namespace Microsoft.AspNetCore.SignalR
{
    public class MessagePackHubProtocolOptions
    {
        public SerializationContext SerializationContext { get; set; } = MessagePackHubProtocol.CreateDefaultSerializationContext();
    }
}
