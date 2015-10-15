// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication
{
    public interface IRemoteAuthenticationEvents
    {
        /// <summary>
        /// Invoked when the remote authentication process has an error.
        /// </summary>
        Task RemoteError(ErrorContext context);

        /// <summary>
        /// Invoked before sign in.
        /// </summary>
        Task TicketReceived(TicketReceivedContext context);
    }
}