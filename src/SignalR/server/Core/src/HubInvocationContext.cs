// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

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
        /// <param name="serviceProvider"></param>
        /// <param name="hub">The instance of the Hub.</param>
        /// <param name="hubMethod"></param>
        /// <param name="hubMethodArguments">The arguments provided by the client.</param>
        public HubInvocationContext(HubCallerContext context, IServiceProvider serviceProvider, Hub hub, MethodInfo hubMethod, object[] hubMethodArguments)
#pragma warning disable CS0618 // Type or member is obsolete
            : this(context, hubMethod.Name, hubMethodArguments)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            Hub = hub;
            ServiceProvider = serviceProvider;
            HubMethod = hubMethod;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="HubInvocationContext"/> class.
        /// </summary>
        /// <param name="context">Context for the active Hub connection and caller.</param>
        /// <param name="hubMethodName">The name of the Hub method being invoked.</param>
        /// <param name="hubMethodArguments">The arguments provided by the client.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative is to use the other constructor.")]
        public HubInvocationContext(HubCallerContext context, string hubMethodName, object[] hubMethodArguments)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            HubMethodName = hubMethodName;
#pragma warning restore CS0618 // Type or member is obsolete
            HubMethodArguments = hubMethodArguments;
            Context = context;
        }

        /// <summary>
        /// Gets the context for the active Hub connection and caller.
        /// </summary>
        public HubCallerContext Context { get; }

        /// <summary>
        /// Gets the Hub instance.
        /// </summary>
        public Hub Hub { get; }

        /// <summary>
        /// Gets the name of the Hub method being invoked.
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is to use HubMethod.Name.")]
        public string HubMethodName { get; }

        /// <summary>
        /// Gets the arguments provided by the client.
        /// </summary>
        public IReadOnlyList<object> HubMethodArguments { get; }

        public IServiceProvider ServiceProvider { get; }

        public MethodInfo HubMethod { get; }
    }
}
