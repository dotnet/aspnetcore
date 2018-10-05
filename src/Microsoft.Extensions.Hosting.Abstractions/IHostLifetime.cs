// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostLifetime
    {
        /// <summary>
        /// Called at the start of <see cref="IHost.StartAsync(CancellationToken)"/> which will wait until it's complete before
        /// continuing. This can be used to delay startup until signaled by an external event.
        /// </summary>
        Task WaitForStartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called from <see cref="IHost.StopAsync(CancellationToken)"/> to indicate that the host as stopped and clean up resources.
        /// </summary>
        /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
