// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A base class for hub messages related to a specific invocation.
    /// </summary>
    public abstract class HubInvocationMessage : HubMessage
    {
        /// <summary>
        /// Gets or sets a name/value collection of headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets the invocation ID.
        /// </summary>
        public string InvocationId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubInvocationMessage"/> class.
        /// </summary>
        /// <param name="invocationId">The invocation ID.</param>
        protected HubInvocationMessage(string invocationId)
        {
            InvocationId = invocationId;
        }
    }
}
