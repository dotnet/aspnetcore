// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets
{
    public class MessagingConnection : Connection
    {
        public override ConnectionMode Mode => ConnectionMode.Messaging;
        public IChannelConnection<Message> Transport { get; }

        public MessagingConnection(string id, IChannelConnection<Message> transport) : base(id)
        {
            Transport = transport;
        }

        public override void Dispose()
        {
            Transport.Dispose();
        }
    }
}
