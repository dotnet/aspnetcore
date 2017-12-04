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
        /// <summary>
        /// The default keep-alive interval. This is set to exactly half of the default client timeout window,
        /// to ensure a ping can arrive in time to satisfy the client timeout.
        /// </summary>
        public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15);

        public JsonSerializerSettings JsonSerializerSettings { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
        public SerializationContext MessagePackSerializationContext { get; set; } = MessagePackHubProtocol.CreateDefaultSerializationContext();
        public TimeSpan NegotiateTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The interval at which keep-alive messages should be sent. The default interval
        /// is 15 seconds.
        /// </summary>
        /// <remarks>
        /// This interval is not used by the Long Polling transport as it has inherent keep-alive
        /// functionality because of the polling mechanism. 
        /// </remarks>
        public TimeSpan KeepAliveInterval { get; set; } = DefaultKeepAliveInterval;
    }
}
