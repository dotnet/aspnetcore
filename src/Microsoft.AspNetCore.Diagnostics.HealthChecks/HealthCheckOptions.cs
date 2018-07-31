// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    /// <summary>
    /// Contains options for the <see cref="HealthCheckMiddleware"/>.
    /// </summary>
    public class HealthCheckOptions
    {
        public IDictionary<HealthCheckStatus, int> ResultStatusCodes { get; } = new Dictionary<HealthCheckStatus, int>()
        {
            { HealthCheckStatus.Healthy, StatusCodes.Status200OK },
            { HealthCheckStatus.Degraded, StatusCodes.Status200OK },
            { HealthCheckStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable },

            // This means that a health check failed, so 500 is appropriate. This is an error.
            { HealthCheckStatus.Failed, StatusCodes.Status500InternalServerError },
        };

        /// <summary>
        /// Gets or sets a delegate used to write the response.
        /// </summary>
        /// <remarks>
        /// The default value is a delegate that will write a minimal <c>text/plain</c> response with the value
        /// of <see cref="CompositeHealthCheckResult.Status"/> as a string.
        /// </remarks>
        public Func<HttpContext, CompositeHealthCheckResult, Task> ResponseWriter { get; set; } = HealthCheckResponseWriters.WriteMinimalPlaintext;
    }
}
