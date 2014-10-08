// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

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
        public ResponseMessageResult([NotNull] HttpResponseMessage response)
            : base(response)
        {
            Response = response;
        }

        /// <summary>
        /// Gets the response message.
        /// </summary>
        public HttpResponseMessage Response { get; private set; }
    }
}