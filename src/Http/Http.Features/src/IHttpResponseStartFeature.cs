// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Feature to start response writing.
    /// </summary>
    public interface IHttpResponseStartFeature
    {
        /// <summary>
        /// Starts the response by calling OnStarting() and making headers unmodifiable.
        /// </summary>
        /// <param name="flush"><c>True</c> if headers and request line should be flushed, otherwise <c>false</c>.</param>
        /// <param name="cancellationToken"></param>
        Task StartAsync(bool flush = false, CancellationToken cancellationToken = default);
    }
}
