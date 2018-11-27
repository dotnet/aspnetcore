// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace FormatterWebSite
{
    public class StringInputFormatter : TextInputFormatter
    {
        public StringInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding effectiveEncoding)
        {
            var request = context.HttpContext.Request;
            using (var reader = new StreamReader(request.Body, effectiveEncoding))
            {
                var stringContent = reader.ReadToEnd();
                return InputFormatterResult.SuccessAsync(stringContent);
            }
        }
    }
}