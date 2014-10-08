// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns a <see cref="System.Net.HttpStatusCode.BadRequest"/> response and performs
    /// content negotiation on an <see cref="HttpError"/> with a <see cref="HttpError.Message"/>.
    /// </summary>
    public class BadRequestErrorMessageResult : ObjectResult
    {
        /// <summary>Initializes a new instance of the <see cref="BadRequestErrorMessageResult"/> class.</summary>
        /// <param name="message">The user-visible error message.</param>
        public BadRequestErrorMessageResult([NotNull] string message)
            : base(new HttpError(message))
        {
            Message = message;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; private set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await base.ExecuteResultAsync(context);
        }
    }
}