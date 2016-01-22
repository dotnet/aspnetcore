// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    public class ContentResult : ActionResult
    {
        private readonly string DefaultContentType = new MediaTypeHeaderValue("text/plain")
        {
            Encoding = Encoding.UTF8
        }.ToString();

        /// <summary>
        /// Gets or set the content representing the body of the response.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get; set; }

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

            string resolvedContentType;
            Encoding resolvedContentTypeEncoding;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                ContentType,
                response.ContentType,
                DefaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (StatusCode != null)
            {
                response.StatusCode = StatusCode.Value;
            }

            logger.ContentResultExecuting(resolvedContentType);

            if (Content != null)
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();

                return response.WriteAsync(Content, resolvedContentTypeEncoding);
            }

            return TaskCache.CompletedTask;
        }
    }
}
