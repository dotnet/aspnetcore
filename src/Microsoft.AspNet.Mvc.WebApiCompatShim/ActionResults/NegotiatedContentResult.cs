// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that performs content negotiation.
    /// </summary>
    /// <typeparam name="T">The type of content in the entity body.</typeparam>
    public class NegotiatedContentResult<T> : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiatedContentResult{T}"/> class with the values provided.
        /// </summary>
        /// <param name="statusCode">The HTTP status code for the response message.</param>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        public NegotiatedContentResult(HttpStatusCode statusCode, T content)
            : base(content)
        {
            StatusCode = (int)statusCode;
            Content = content;
        }

        /// <summary>
        /// Gets the content value to negotiate and format in the entity body.
        /// </summary>
        public T Content { get; private set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)StatusCode;
            return base.ExecuteResultAsync(context);
        }
    }
}