// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// Represents an action result that performs route generation and content negotiation and returns a
    /// <see cref="HttpStatusCode.Created"/> response when content negotiation succeeds.
    /// </summary>
    /// <typeparam name="T">The type of content in the entity body.</typeparam>
    public class CreatedNegotiatedContentResult<T> : NegotiatedContentResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedNegotiatedContentResult{T}"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="location">The location at which the content has been created.</param>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        public CreatedNegotiatedContentResult(Uri location, T content)
            : base(HttpStatusCode.Created, content)
        {
            Location = location;
        }

        /// <summary>
        /// Gets the location at which the content has been created.
        /// </summary>
        public Uri Location { get; private set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            string location;
            if (Location.IsAbsoluteUri)
            {
                location = Location.AbsoluteUri;
            }
            else
            {
                location = Location.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }

            context.HttpContext.Response.Headers.Add("Location", new string[] { location });

            return base.ExecuteResultAsync(context);
        }
    }
}