// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// Represents an action result that performs content negotiation and returns an <see cref="HttpStatusCode.OK"/>
    /// response when it succeeds.
    /// </summary>
    /// <typeparam name="T">The type of content in the entity body.</typeparam>
    public class OkNegotiatedContentResult<T> : NegotiatedContentResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OkNegotiatedContentResult{T}"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        /// <param name="formatters">The formatters to use to negotiate and format the content.</param>
        public OkNegotiatedContentResult([NotNull] T content)
            : base(HttpStatusCode.OK, content)
        {
        }
    }
}