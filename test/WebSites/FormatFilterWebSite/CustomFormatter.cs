// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;

namespace FormatFilterWebSite
{
    public class CustomFormatter : OutputFormatter
    {
        public string ContentType { get; private set; }

        public CustomFormatter(string contentType)
        {
            ContentType = contentType;
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(contentType));
            SupportedEncodings.Add(Encoding.GetEncoding("utf-8"));
        }

        public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            if (base.CanWriteResult(context, contentType))
            {
                var actionReturn = context.Object as Product;
                if (actionReturn != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.ContentType = ContentType + ";charset=utf-8";
            await response.WriteAsync(context.Object.ToString());
        }
    }
}