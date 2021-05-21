// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A proxy abstraction for invoking hub methods.
    /// </summary>
    public interface IClientProxy
    {
        // client proxy method is called SendCoreAsync instead of SendAsync so that arrays of references
        // like string[], e.g. SendAsync(string, string[]), do not choose SendAsync(string, object[])
        // over SendAsync(string, object) overload

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="method">Name of the method to invoke.</param>
        /// <param name="args">A collection of arguments to pass to the client.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
        Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default);
    }
}
