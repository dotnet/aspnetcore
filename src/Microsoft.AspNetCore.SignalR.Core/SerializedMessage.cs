// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    public readonly struct SerializedMessage
    {
        public string ProtocolName { get; }
        public ReadOnlyMemory<byte> Serialized { get; }

        public SerializedMessage(string protocolName, ReadOnlyMemory<byte> serialized)
        {
            ProtocolName = protocolName;
            Serialized = serialized;
        }
    }
}