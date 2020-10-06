// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for <see cref="StatusCodePagesMiddleware"/>.
    /// </summary>
    public class StatusCodePagesOptions
    {
        /// <summary>
        /// Creates a default <see cref="StatusCodePagesOptions"/> which produces a plaintext response
        /// containing the status code and the reason phrase.
        /// </summary>
        public StatusCodePagesOptions()
        {
            HandleAsync = context =>
            {
                // TODO: Render with a pre-compiled html razor view.
                var statusCode = context.HttpContext.Response.StatusCode;

                var body = BuildResponseBody(statusCode);

                context.HttpContext.Response.ContentType = "text/plain";
                return context.HttpContext.Response.WriteAsync(body);
            };
        }

        private string BuildResponseBody(int httpStatusCode)
        {
            // Note the 500 spaces are to work around an IE 'feature'
            var internetExplorerWorkaround = new string(' ', 500);

            var reasonPhrase = ReasonPhrases.GetReasonPhrase(httpStatusCode);

            return string.Format(CultureInfo.InvariantCulture, "Status Code: {0}{1}{2}{3}",
                                                                    httpStatusCode,
                                                                    string.IsNullOrWhiteSpace(reasonPhrase) ? "" : "; ",
                                                                    reasonPhrase,
                                                                    internetExplorerWorkaround);
        }

        /// <summary>
        /// The handler that generates the response body for the given <see cref="StatusCodeContext"/>. By default this produces a plain text response that includes the status code.
        /// </summary>
        public Func<StatusCodeContext, Task> HandleAsync { get; set; }
    }
}
