// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// The <see cref="CancelInvocationMessage"/> represents a cancellation of a streaming method.
    /// </summary>
    public class CancelInvocationMessage : HubInvocationMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancelInvocationMessage"/> class.
        /// </summary>
        /// <param name="invocationId">The ID of the hub method invocation being canceled.</param>
        public CancelInvocationMessage(string invocationId) : base(invocationId)
        {
        }
    }
}
