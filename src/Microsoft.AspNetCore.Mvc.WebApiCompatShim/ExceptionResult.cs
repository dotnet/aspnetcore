// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns a <see cref="StatusCodes.Status500InternalServerError"/> response and
    /// performs content negotiation on an <see cref="HttpError"/> based on an <see cref="Exception"/>.
    /// </summary>
    public class ExceptionResult : ObjectResult
    {
        /// <summary>Initializes a new instance of the <see cref="ExceptionResult"/> class.</summary>
        /// <param name="exception">The exception to include in the error.</param>
        /// <param name="includeErrorDetail">
        /// <see langword="true"/> if the error should include exception messages; otherwise, <see langword="false"/>.
        /// </param>
        public ExceptionResult(Exception exception, bool includeErrorDetail)
            : base(new HttpError(exception, includeErrorDetail))
        {
            Exception = exception;
            IncludeErrorDetail = includeErrorDetail;
        }

        /// <summary>
        /// Gets the exception to include in the error.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the error should include exception messages.
        /// </summary>
        public bool IncludeErrorDetail { get; private set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return base.ExecuteResultAsync(context);
        }
    }
}