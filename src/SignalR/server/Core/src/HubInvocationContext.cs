// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A context passed to <see cref="IAuthorizationHandler"/>'s for custom Authorization on Hub methods.
    /// </summary>
    public class HubInvocationContext
    {
        /// <summary>
        /// Instantiates a new <see cref="HubInvocationContext"/> instance.
        /// </summary>
        /// <param name="context">The context that provides information about the hub connection.</param>
        /// <param name="hubMethodName">The name of the hub method being invoked.</param>
        /// <param name="hubMethodArguments">The arguments provided by the client to call the hub method.</param>
        public HubInvocationContext(HubCallerContext context, string hubMethodName, object[] hubMethodArguments)
        {
            HubMethodName = hubMethodName;
            HubMethodArguments = hubMethodArguments;
            Context = context;
        }

        /// <summary>
        /// The context that provides information about the hub connection.
        /// </summary>
        public HubCallerContext Context { get; }

        /// <summary>
        /// The name of the hub method being invoked.
        /// </summary>
        public string HubMethodName { get; }

        /// <summary>
        /// The arguments provided by the client to call the hub method.
        /// </summary>
        public IReadOnlyList<object> HubMethodArguments { get; }
    }
}
