// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubProtocolExtensions
    {
        public static byte[] WriteToArray(this IHubProtocol hubProtocol, HubMessage message)
        {
            using (var ms = new LimitArrayPoolWriteStream())
            {
                hubProtocol.WriteMessage(message, ms);
                return ms.ToArray();
            }
        }
    }
}
