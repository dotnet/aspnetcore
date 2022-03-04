// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    internal static class HubMessageHelpers
    {
        // This lets you add headers to a hub message and return it, in a single expression.
        public static HubMessage AddHeaders(IDictionary<string, string> headers, HubInvocationMessage hubMessage)
        {
            foreach (var header in headers)
            {
                if (hubMessage.Headers == null)
                {
                    hubMessage.Headers = new Dictionary<string, string>();
                }

                hubMessage.Headers[header.Key] = header.Value;
            }

            return hubMessage;
        }
    }
}
