// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A formatter which selects itself when content-negotiation has failed and writes a 406 Not Acceptable response.
    /// </summary>
    public class HttpNotAcceptableOutputFormatter : IOutputFormatter
    {
        /// <inheritdoc />
        public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            return context.FailedContentNegotiation ?? false;
        }

        /// <inheritdoc />
        public Task WriteAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.StatusCode = StatusCodes.Status406NotAcceptable;
            return Task.FromResult(true);
        }
    }
}