// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Options for StatusCodePagesMiddleware.
    /// </summary>
    public class StatusCodePagesOptions
    {
        public StatusCodePagesOptions()
        {
            HandleAsync = context =>
            {
                // TODO: Render with a pre-compiled html razor view.
                // Note the 500 spaces are to work around an IE 'feature'
                var statusCode = context.HttpContext.Response.StatusCode;
                var body = string.Format(CultureInfo.InvariantCulture, "Status Code: {0}; {1}",
                    statusCode, ReasonPhrases.GetReasonPhrase(statusCode)) + new string(' ', 500);
                return context.HttpContext.Response.SendAsync(body, "text/plain");
            };
        }

        public Func<StatusCodeContext, Task> HandleAsync { get; set; }
    }
}