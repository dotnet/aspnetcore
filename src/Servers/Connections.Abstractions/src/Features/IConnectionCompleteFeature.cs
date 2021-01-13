// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Represents the completion action for a connection.
    /// </summary>
    public interface IConnectionCompleteFeature
    {
        /// <summary>
        /// Registers a callback to be invoked after a connection has fully completed processing. This is
        /// intended for resource cleanup.
        /// </summary>
        /// <param name="callback">The callback to invoke after the connection has completed processing.</param>
        /// <param name="state">The state to pass into the callback.</param>
        void OnCompleted(Func<object, Task> callback, object state);
    }
}
