// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public interface IHubProtocol
    {
        string Name { get; }

        int Version { get; }

        TransferFormat TransferFormat { get; }

        bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message);

        void WriteMessage(HubMessage message, Stream output);

        bool IsVersionSupported(int version);
    }
}
