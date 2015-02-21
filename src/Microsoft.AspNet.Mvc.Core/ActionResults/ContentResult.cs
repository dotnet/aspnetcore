// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResult : ActionResult
    {
        public string Content { get; set; }

        public Encoding ContentEncoding { get; set; }

        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;

            MediaTypeHeaderValue contentTypeHeader;
            if (string.IsNullOrEmpty(ContentType))
            {
                contentTypeHeader = new MediaTypeHeaderValue("text/plain");
            }
            else
            {
                contentTypeHeader = new MediaTypeHeaderValue(ContentType);
            }

            contentTypeHeader.Encoding = ContentEncoding ?? Encodings.UTF8EncodingWithoutBOM;
            response.ContentType = contentTypeHeader.ToString();

            if (StatusCode != null)
            {
                response.StatusCode = StatusCode.Value;
            }

            if (Content != null)
            {
                await response.WriteAsync(Content, contentTypeHeader.Encoding);
            }
        }
    }
}
