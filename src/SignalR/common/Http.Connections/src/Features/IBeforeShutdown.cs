// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections.Features
{
    public interface IBeforeShutdown
    {
        /// <summary>
        /// Register a function to run when graceful shutdown starts and before connections are closed.
        /// </summary>
        /// <param name="func">Function to run on graceful shutdown.</param>
        /// <returns>Disposable that will remove the registered function.</returns>
        IDisposable Register(Func<Task> func);
    }
}
