// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubOptions
    {
        public JsonSerializerSettings JsonSerializerSettings { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
        public SerializationContext MessagePackSerializationContext { get; set; } = MessagePackHubProtocol.CreateDefaultSerializationContext();
        public TimeSpan NegotiateTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
