// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BasicWebSite.Formatters;

public class PlainTextFormatter : TextOutputFormatter
{
    public PlainTextFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
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
        response.ContentType = "text/plain;charset=utf-8";
        await response.WriteAsync(context.Object as string);
    }
}
