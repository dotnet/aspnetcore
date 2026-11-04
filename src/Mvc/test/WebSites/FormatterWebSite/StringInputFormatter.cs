// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace FormatterWebSite;

public class StringInputFormatter : TextInputFormatter
{
    public StringInputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding effectiveEncoding)
    {
        var request = context.HttpContext.Request;
        using (var reader = new StreamReader(request.Body, effectiveEncoding))
        {
            var stringContent = await reader.ReadToEndAsync();
            return await InputFormatterResult.SuccessAsync(stringContent);
        }
    }
}
