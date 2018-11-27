// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Used by protocol serializers/deserializers to transfer information about streaming parameters.
    /// Is packed as an argument in the form `{"streamId": "42"}`, and sent over wire.
    /// Is then unpacked on the other side, and a new channel is created and saved under the streamId.
    /// Then, each <see cref="StreamDataMessage"/> is routed to the appropiate channel based on streamId.
    /// </summary>
    public class StreamPlaceholder
    {
        public string StreamId { get; private set; }

        public StreamPlaceholder(string streamId)
        {
            StreamId = streamId;
        }
    }
}
