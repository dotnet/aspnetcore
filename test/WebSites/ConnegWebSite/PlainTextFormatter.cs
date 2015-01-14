// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;

namespace ConnegWebSite
{
    public class PlainTextFormatter : OutputFormatter
    {
        public PlainTextFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
            SupportedEncodings.Add(Encoding.GetEncoding("utf-8"));
        }

        public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            if (base.CanWriteResult(context, contentType))
            {
                var actionReturnString = context.Object as string;
                if (actionReturnString != null)
                {
                    return true;
                }
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.ContentType = "text/plain;charset=utf-8";
            await response.WriteAsync(context.Object as string);
        }
    }
}