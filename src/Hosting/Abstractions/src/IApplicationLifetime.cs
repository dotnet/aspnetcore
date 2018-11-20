// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    public interface IApplicationLifetime
    {
        /// <summary>
        /// Triggered when the application host has fully started and is about to wait
        /// for a graceful shutdown.
        /// </summary>
        CancellationToken ApplicationStarted { get; }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Requests may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        CancellationToken ApplicationStopping { get; }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        CancellationToken ApplicationStopped { get; }

        /// <summary>
        /// Requests termination of the current application.
        /// </summary>
        void StopApplication();
    }
}
