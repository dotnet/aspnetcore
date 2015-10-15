// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication
{
    public class RemoteAuthenticationEvents : IRemoteAuthenticationEvents
    {
        public Func<ErrorContext, Task> OnRemoteError { get; set; } = context => Task.FromResult(0);

        public Func<TicketReceivedContext, Task> OnTicketReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when there is a remote error
        /// </summary>
        public virtual Task RemoteError(ErrorContext context) => OnRemoteError(context);

        /// <summary>
        /// Invoked after the remote ticket has been recieved.
        /// </summary>
        public virtual Task TicketReceived(TicketReceivedContext context) => OnTicketReceived(context);
    }
}