// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A Class that represents RequestDelegate metadata.
    /// </summary>

    public sealed class RequestDelegateMetadata
    {
        /// <summary>
        /// List of request delgate metadata
        /// </summary>
        public List<object> EndpointMetadata { get; set; } = new();
    }

}
