// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Context for a Hub invocation.
    /// </summary>
    public class HubInvocationContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="HubInvocationContext"/> class.
        /// </summary>
        /// <param name="context">Context for the active Hub connection and caller.</param>
        /// <param name="hubType">The type of the Hub.</param>
        /// <param name="hubMethodName">The name of the Hub method being invoked.</param>
        /// <param name="hubMethodArguments">The arguments provided by the client.</param>
        public HubInvocationContext(HubCallerContext context, Type hubType, string hubMethodName, object[] hubMethodArguments): this(context, hubMethodName, hubMethodArguments)
        {
            HubType = hubType;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="HubInvocationContext"/> class.
        /// </summary>
        /// <param name="context">Context for the active Hub connection and caller.</param>
        /// <param name="hubMethodName">The name of the Hub method being invoked.</param>
        /// <param name="hubMethodArguments">The arguments provided by the client.</param>
        public HubInvocationContext(HubCallerContext context, string hubMethodName, object[] hubMethodArguments)
        {
            HubMethodName = hubMethodName;
            HubMethodArguments = hubMethodArguments;
            Context = context;
        }

        /// <summary>
        /// Gets the context for the active Hub connection and caller.
        /// </summary>
        public HubCallerContext Context { get; }

        /// <summary>
        /// Gets the Hub type.
        /// </summary>
        public Type HubType { get; }

        /// <summary>
        /// Gets the name of the Hub method being invoked.
        /// </summary>
        public string HubMethodName { get; }

        /// <summary>
        /// Gets the arguments provided by the client.
        /// </summary>
        public IReadOnlyList<object> HubMethodArguments { get; }
    }
}
