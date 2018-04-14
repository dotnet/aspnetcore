// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public static class HubProtocolExtensions
    {
        // Would work as default interface impl
        public static byte[] GetMessageBytes(this IHubProtocol hubProtocol, HubMessage message)
        {
            var writer = MemoryBufferWriter.Get();
            try
            {
                hubProtocol.WriteMessage(message, writer);
                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }
    }
}
