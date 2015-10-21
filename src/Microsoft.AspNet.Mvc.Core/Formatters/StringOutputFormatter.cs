// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Always writes a string value to the response, regardless of requested content type.
    /// </summary>
    public class StringOutputFormatter : OutputFormatter
    {
        public StringOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain").CopyAsReadOnly());
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Ignore the passed in content type, if the object is string
            // always return it as a text/plain format.
            if (context.ObjectType == typeof(string) || context.Object is string)
            {
                context.ContentType = SupportedMediaTypes[0];
                return true;
            }

            return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var valueAsString = (string)context.Object;
            if (string.IsNullOrEmpty(valueAsString))
            {
                return TaskCache.CompletedTask;
            }

            var response = context.HttpContext.Response;

            return response.WriteAsync(valueAsString, context.ContentType?.Encoding ?? Encoding.UTF8);
        }
    }
}
