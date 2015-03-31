// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.Hosting
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    public class ApplicationLifetime : IApplicationLifetime
    {
        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Request may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        /// <returns></returns>
        public CancellationToken ApplicationStopping
        {
            get { return _stoppingSource.Token; }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        /// <returns></returns>
        public CancellationToken ApplicationStopped
        {
            get { return _stoppedSource.Token; }
        }

        /// <summary>
        /// Signals the ApplicationStopping event and blocks until it completes.
        /// </summary>
        public void NotifyStopping()
        {
            try
            {
                _stoppingSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception)
            {
                // TODO: LOG
            }
        }

        /// <summary>
        /// Signals the ApplicationStopped event and blocks until it completes.
        /// </summary>
        public void NotifyStopped()
        {
            try
            {
                _stoppedSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception)
            {
                // TODO: LOG
            }
        }
    }
}