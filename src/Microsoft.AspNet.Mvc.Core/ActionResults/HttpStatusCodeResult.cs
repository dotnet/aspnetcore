// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// produce an HTTP response with the given response status code.
    /// </summary>
    public class HttpStatusCodeResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpStatusCodeResult"/> class
        /// with the given <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public HttpStatusCodeResult(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; private set; }

        /// <inheritdoc />
        public override void ExecuteResult([NotNull] ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCode;
        }
    }
}
