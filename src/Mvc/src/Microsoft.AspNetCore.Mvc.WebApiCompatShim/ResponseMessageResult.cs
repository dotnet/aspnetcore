// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns a specified response message.
    /// </summary>
    public class ResponseMessageResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMessageResult"/> class.
        /// </summary>
        /// <param name="response">The response message.</param>
        public ResponseMessageResult(HttpResponseMessage response)
            : base(response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            Response = response;
        }

        /// <summary>
        /// Gets the response message.
        /// </summary>
        public HttpResponseMessage Response { get; private set; }
    }
}