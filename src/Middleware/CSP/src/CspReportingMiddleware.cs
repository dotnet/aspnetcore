// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// CSP middleware used to collect violation reports sent by user agents. Enabled automatically when a relative reporting URI is set on the CSP policy.
    /// </summary>
    public class CspReportingMiddleware
    {
        private readonly CspReportLogger _cspReportLogger;

        /// <summary>
        /// Instantiates a new <see cref="CspReportingMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="reportLogger">A custom logger that allows extending this middleware's logging capabilities.</param>
        public CspReportingMiddleware(RequestDelegate next, CspReportLogger reportLogger)
        {
            _cspReportLogger = reportLogger;
        }

        private bool IsReportRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments(_cspReportLogger.ReportUri)
                && request.Method == HttpMethods.Post
                && request.ContentType?.StartsWith(CspConstants.CspReportContentType) == true
                && request.ContentLength != 0;
        }


        /// <summary>
        /// Handle incoming violation reports. Returns a 204 response regardless of whether the report is valid.
        /// </summary>
        public Task Invoke(HttpContext context)
        {
            if (IsReportRequest(context.Request))
            {
                _cspReportLogger.Log(context.Request.Body);
            }

            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return Task.FromResult(0);
        }
    }
}
