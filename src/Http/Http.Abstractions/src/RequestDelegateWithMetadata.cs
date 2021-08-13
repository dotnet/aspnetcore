// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A Class that represents RequestDelegate with associated metadata.
    /// </summary>

    public sealed class RequestDelegateResult
    {
        /// <summary>
        /// A function that can process an HTTP request.
        /// </summary>
        /// <returns>A task that represents the completion of request processing.</returns>

        public RequestDelegate? RequestDelegate { get; set; }

        /// <summary>
        /// List of request delgate metadata
        /// </summary>
        public List<object> EndpointMetadata { get; set; } = new();
    }

}
