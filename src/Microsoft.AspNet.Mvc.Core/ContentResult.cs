// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResult : ActionResult
    {
        private readonly MediaTypeHeaderValue DefaultContentType = new MediaTypeHeaderValue("text/plain")
        {
            Encoding = Encoding.UTF8
        };

        /// <summary>
        /// Gets or set the content representing the body of the response.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MediaTypeHeaderValue"/> representing the Content-Type header of the response.
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ContentResult>();

            var response = context.HttpContext.Response;
            var contentTypeHeader = ContentType;

            if (contentTypeHeader != null && contentTypeHeader.Encoding == null)
            {
                // Do not modify the user supplied content type, so copy it instead
                contentTypeHeader = contentTypeHeader.Copy();
                contentTypeHeader.Encoding = Encoding.UTF8;
            }

            response.ContentType = contentTypeHeader?.ToString()
                ?? response.ContentType
                ?? DefaultContentType.ToString();

            if (StatusCode != null)
            {
                response.StatusCode = StatusCode.Value;
            }

            logger.ContentResultExecuting(contentTypeHeader);

            if (Content != null)
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();

                return response.WriteAsync(Content, contentTypeHeader?.Encoding ?? DefaultContentType.Encoding);
            }

            return TaskCache.CompletedTask;
        }
    }
}
