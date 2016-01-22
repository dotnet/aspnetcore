// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using ShimResources = Microsoft.AspNetCore.Mvc.WebApiCompatShim.Resources;

namespace System.Web.Http
{
    public class HttpResponseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        /// <param name="statusCode">The status code of the response.</param>
        public HttpResponseException(HttpStatusCode statusCode)
            : this(new HttpResponseMessage(statusCode))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        /// <param name="response">The response message.</param>
        public HttpResponseException(HttpResponseMessage response)
            : base(ShimResources.HttpResponseExceptionMessage)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            Response = response;
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> to return to the client.
        /// </summary>
        public HttpResponseMessage Response { get; private set; }
    }
}