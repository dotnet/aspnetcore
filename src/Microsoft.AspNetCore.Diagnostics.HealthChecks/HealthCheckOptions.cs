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
        /// <summary>
        /// Gets or sets a predicate that is used to filter the set of health checks executed.
        /// </summary>
        /// <remarks>
        /// If <see cref="Predicate"/> is <c>null</c>, the <see cref="HealthCheckMiddleware"/> will run all
        /// registered health checks - this is the default behavior. To run a subset of health checks,
        /// provide a function that filters the set of checks.
        /// </remarks>
        public Func<HealthCheckRegistration, bool> Predicate { get; set; }

        /// <summary>
        /// Gets a dictionary mapping the <see cref="HealthStatus"/> to an HTTP status code applied to the response.
        /// This property can be used to configure the status codes returned for each status.
        /// </summary>
        public IDictionary<HealthStatus, int> ResultStatusCodes { get; } = new Dictionary<HealthStatus, int>()
        {
            { HealthStatus.Healthy, StatusCodes.Status200OK },
            { HealthStatus.Degraded, StatusCodes.Status200OK },
            { HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable },

            // This means that a health check failed, so 500 is appropriate. This is an error.
            { HealthStatus.Failed, StatusCodes.Status500InternalServerError },
        };

        /// <summary>
        /// Gets or sets a delegate used to write the response.
        /// </summary>
        /// <remarks>
        /// The default value is a delegate that will write a minimal <c>text/plain</c> response with the value
        /// of <see cref="HealthReport.Status"/> as a string.
        /// </remarks>
        public Func<HttpContext, HealthReport, Task> ResponseWriter { get; set; } = HealthCheckResponseWriters.WriteMinimalPlaintext;

        /// <summary>
        /// Gets or sets a value that controls whether the health check middleware will add HTTP headers to prevent
        /// response caching. If the value is <c>false</c> the health check middleware will set or override the 
        /// <c>Cache-Control</c>, <c>Expires</c>, and <c>Pragma</c> headers to prevent response caching. If the value 
        /// is <c>true</c> the health check middleware will not modify the cache headers of the response.
        /// </summary>
        public bool SuppressCacheHeaders { get; set; }
    }
}
