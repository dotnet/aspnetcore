// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Owin;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Properties;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>Represents an action result that performs content negotiation.</summary>
    /// <typeparam name="T">The type of content in the entity body.</typeparam>
    internal class NegotiatedContentResult : IActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiatedContentResult{T}"/> class with the values provided.
        /// </summary>
        /// <param name="statusCode">The HTTP status code for the response message.</param>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        /// <param name="contentNegotiator">The content negotiator to handle content negotiation.</param>
        /// <param name="request">The request message which led to this result.</param>
        /// <param name="formatters">The formatters to use to negotiate and format the content.</param>
        public NegotiatedContentResult(HttpStatusCode statusCode,
                                       Type declaredType,
                                       object content,
                                       IOwinContentNegotiator contentNegotiator,
                                       IOwinContext owinContext,
                                       IEnumerable<MediaTypeFormatter> formatters)
        {
            Contract.Assert(content != null);
            Contract.Assert(declaredType != null);
            Contract.Assert(owinContext != null);
            Contract.Assert(formatters != null);

            StatusCode = statusCode;
            DeclaredType = declaredType;
            Content = content;
            CurrentOwinContext = owinContext;
            Formatters = formatters;
            ContentNegotiator = contentNegotiator;
        }

        /// <summary>Gets the HTTP status code for the response message.</summary>
        public HttpStatusCode StatusCode { get; private set; }

        public Type DeclaredType { get; private set; }

        /// <summary>Gets the content value to negotiate and format in the entity body.</summary>
        public object Content { get; private set; }

        /// <summary>Gets the content negotiator to handle content negotiation.</summary>
        public IOwinContentNegotiator ContentNegotiator { get; private set; }

        /// <summary>Gets the request message which led to this result.</summary>
        public IOwinContext CurrentOwinContext { get; private set; }

        /// <summary>Gets the formatters to use to negotiate and format the content.</summary>
        public IEnumerable<MediaTypeFormatter> Formatters { get; private set; }

        /// <inheritdoc />
        public virtual Task ExecuteResultAsync(RequestContext context)
        {
            // Run content negotiation.
            ContentNegotiationResult result = ContentNegotiator.Negotiate(DeclaredType, CurrentOwinContext, Formatters);

            if (result == null)
            {
                // A null result from content negotiation indicates that the response should be a 406.
                CurrentOwinContext.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;

                return Task.FromResult(false);
            }
            else
            {
                IOwinResponse response = CurrentOwinContext.Response;
                response.StatusCode = (int)StatusCode;
                Contract.Assert(result.Formatter != null);

                var objectContent = new ObjectContent(DeclaredType, Content, result.Formatter, result.MediaType);

                // Copy non-content headers
                IDictionary<string, string[]> responseHeaders = response.Headers;
                foreach (KeyValuePair<string, string[]> header in response.Headers)
                {
                    responseHeaders[header.Key] = header.Value.AsArray();
                }

                // Copy content headers
                foreach (KeyValuePair<string, IEnumerable<string>> contentHeader in objectContent.Headers)
                {
                    responseHeaders[contentHeader.Key] = contentHeader.Value.AsArray();
                }

                return objectContent.CopyToAsync(response.Body);
            }
        }
    }
}
