// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubProtocolWriteMessageExtensions
    {
        public static byte[] WriteToArray(this IHubProtocol protocol, HubMessage message)
        {
            using (var output = new MemoryStream())
            {
                // Encode the message
                if (!protocol.TryWriteMessage(message, output))
                {
                    throw new InvalidOperationException("Failed to write message to the output stream");
                }
                
                return output.ToArray();
            }
        }
    }
}
