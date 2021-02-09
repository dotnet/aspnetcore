// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Api
{
    /// <summary>
    /// Defines a contract that represents the result of an HTTP endpoint.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Write an HTTP response reflecting the result.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the asynchronous execute operation.</returns>
        Task ExecuteAsync(HttpContext httpContext);
    }
}
