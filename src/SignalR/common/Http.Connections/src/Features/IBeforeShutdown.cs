// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Connections.Features
{
    /// <summary>
    /// Feature that allows registering functions to run on application closing before connections are closed.
    /// </summary>
    public interface IBeforeShutdown
    {
        /// <summary>
        /// Register a function to run when graceful shutdown starts and before connections are closed.
        /// </summary>
        /// <remarks>
        /// The Server or Host may forcefully close connections before this task completes.
        /// </remarks>
        /// <param name="callback">Function to run on graceful shutdown.</param>
        /// <returns>Disposable that will remove the registered function.</returns>
        IDisposable Register(Func<Task> callback);

        /// <summary>
        /// Trigger the registered callbacks.
        /// </summary>
        /// <returns>Task representing the callbacks running.</returns>
        Task TriggerAsync();
    }
}
