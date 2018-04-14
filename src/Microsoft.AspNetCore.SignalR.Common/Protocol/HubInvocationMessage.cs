// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public abstract class HubInvocationMessage : HubMessage
    {
        public IDictionary<string, string> Headers { get; set; }

        public string InvocationId { get; }

        protected HubInvocationMessage(string invocationId)
        {
            InvocationId = invocationId;
        }
    }
}
