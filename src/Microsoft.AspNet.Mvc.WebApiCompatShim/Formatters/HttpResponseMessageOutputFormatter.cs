// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpResponseMessageOutputFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            return context.Object is HttpResponseMessage;
        }

        public IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType,
            Type runtimeType,
            MediaTypeHeaderValue contentType)
        {
            return null;
        }

        public async Task WriteAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;

            var responseMessage = context.Object as HttpResponseMessage;
            if (responseMessage == null)
            {
                var message = Resources.FormatHttpResponseMessageFormatter_UnsupportedType(
                    nameof(HttpResponseMessageOutputFormatter),
                    nameof(HttpResponseMessage));

                throw new InvalidOperationException(message);
            }

            using (responseMessage)
            {
                response.StatusCode = (int)responseMessage.StatusCode;

                var responseFeature = context.ActionContext.HttpContext.GetFeature<IHttpResponseFeature>();
                if (responseFeature != null)
                {
                    responseFeature.ReasonPhrase = responseMessage.ReasonPhrase;
                }

                var responseHeaders = responseMessage.Headers;
                foreach (var header in responseHeaders)
                {
                    response.Headers.AppendValues(header.Key, header.Value.ToArray());
                }

                if (responseMessage.Content != null)
                {
                    var contentHeaders = responseMessage.Content.Headers;
                    foreach (var header in contentHeaders)
                    {
                        response.Headers.AppendValues(header.Key, header.Value.ToArray());
                    }

                    await responseMessage.Content.CopyToAsync(response.Body);
                }
            }
        }
    }
}
