// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostLifetime
    {
        /// <summary>
        /// Called at the start of <see cref="IHost.StartAsync(CancellationToken)"/> which will wait until the callback is invoked before
        /// continuing. This can be used to delay startup until signaled by an external event.
        /// </summary>
        /// <param name="callback">A callback that will be invoked when the host should continue.</param>
        /// <param name="state">State to pass to the callback.</param>
        void RegisterDelayStartCallback(Action<object> callback, object state);

        /// <summary>
        /// Called at the start of <see cref="IHost.StartAsync(CancellationToken)"/> to register the given callback for initiating the
        /// application shutdown process.
        /// </summary>
        /// <param name="callback">A callback to invoke when an external signal indicates the application should stop.</param>
        /// <param name="state">State to pass to the callback.</param>
        void RegisterStopCallback(Action<object> callback, object state);

        /// <summary>
        /// Called from <see cref="IHost.StopAsync(CancellationToken)"/> to indicate that the host as stopped and clean up resources.
        /// </summary>
        /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
