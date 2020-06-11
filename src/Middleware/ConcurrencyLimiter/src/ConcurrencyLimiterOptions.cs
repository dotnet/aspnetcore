// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    /// <summary>
    /// Specifies options for the <see cref="ConcurrencyLimiterMiddleware"/>.
    /// </summary>
    public class ConcurrencyLimiterOptions
    {
        /// <summary>
        /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
        /// If it doesn't modify the response, an empty 503 response will be written.
        /// </summary>
        public RequestDelegate OnRejected { get; set; } = context =>
        {
            return Task.CompletedTask;
        };
    }
}
