// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubProtocolExtensions
    {
        public static byte[] WriteToArray(this IHubProtocol hubProtocol, HubMessage message)
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
