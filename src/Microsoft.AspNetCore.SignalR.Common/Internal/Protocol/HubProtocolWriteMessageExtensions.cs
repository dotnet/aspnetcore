// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                protocol.WriteMessage(message, output);

                return output.ToArray();
            }
        }
    }
}
