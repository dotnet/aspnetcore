// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// An abstraction that controls when the client attempts to reconnect and how many times it does so.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// If passed to <see cref="HubConnectionBuilderExtensions.WithAutomaticReconnect(IHubConnectionBuilder, IRetryPolicy)"/>,
        /// this will be called after the trasnport loses a connection to determine if and for how long to wait before the next reconnect attempt.
        /// </summary>
        /// <param name="retryContext">
        /// Information related to the next possible reconnect attempt including the number of consecutive failed retries so far, time spent
        /// reconnecting so far and the error that lead to this reconnect attempt.
        /// </param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the amount of time to wait from now before starting the next reconnect attempt.
        /// <see langword="null" /> tells the client to stop retrying and close.
        /// </returns>
        TimeSpan? NextRetryDelay(RetryContext retryContext);
    }
}
