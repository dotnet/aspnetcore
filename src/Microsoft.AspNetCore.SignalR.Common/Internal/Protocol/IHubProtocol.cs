// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public interface IHubProtocol
    {
        MessageType MessageType { get; }

        HubMessage ParseMessage(ReadOnlySpan<byte> input, IInvocationBinder binder);

        bool TryWriteMessage(HubMessage message, IOutput output);
    }
}
