// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Always writes a string value to the response, regardless of requested content type.
    /// </summary>
    public class StringOutputFormatter : OutputFormatter
    {
        public StringOutputFormatter()
        {
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
        }

        public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            // Ignore the passed in content type, if the object is string
            // always return it as a text/plain format.
            if (context.DeclaredType == typeof(string))
            {
                return true;
            }

            if (context.Object is string)
            {
                return true;
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterContext context)
        {
            var valueAsString = (string)context.Object;
            if (string.IsNullOrEmpty(valueAsString))
            {
                return;
            }

            var response = context.HttpContext.Response;

            await response.WriteAsync(valueAsString, context.SelectedEncoding);
        }
    }
}
