// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// A formatter which selects itself when content-negotiation has failed and writes a 406 Not Acceptable response.
    /// </summary>
    public class HttpNotAcceptableOutputFormatter : IOutputFormatter
    {
        /// <inheritdoc />
        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return context.FailedContentNegotiation ?? false;
        }

        /// <inheritdoc />
        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status406NotAcceptable;
            return Task.FromResult(true);
        }
    }
}