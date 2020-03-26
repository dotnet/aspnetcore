// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// Represents an instance of Node.js to which Remote Procedure Calls (RPC) may be sent.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public interface INodeInstance : IDisposable
    {
        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <param name="moduleName">The path to the Node.js module (i.e., JavaScript file) relative to your project root that contains the code to be invoked.</param>
        /// <param name="exportNameOrNull">If set, specifies the CommonJS export to be invoked. If not set, the module's default CommonJS export itself must be a function to be invoked.</param>
        /// <param name="args">Any sequence of JSON-serializable arguments to be passed to the Node.js function.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args);
    }
}
