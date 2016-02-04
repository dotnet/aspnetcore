// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BasicWebSite.Formatters
{
    public class CustomFormatter : TextOutputFormatter
    {
        public string ContentType { get; private set; }

        public CustomFormatter(string contentType)
        {
            ContentType = contentType;
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(contentType));
            SupportedEncodings.Add(Encoding.GetEncoding("utf-8"));
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (base.CanWriteResult(context))
            {
                var actionReturnString = context.Object as string;
                if (actionReturnString != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            response.ContentType = ContentType + ";charset=utf-8";
            await response.WriteAsync(context.Object.ToString());
        }
    }
}