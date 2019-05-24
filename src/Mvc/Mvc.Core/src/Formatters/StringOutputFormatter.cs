// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> for simple text content.
    /// </summary>
    public class StringOutputFormatter : TextOutputFormatter
    {
        public StringOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add("text/plain");
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ObjectType == typeof(string) || context.Object is string)
            {
                // Call into base to check if the current request's content type is a supported media type.
                return base.CanWriteResult(context);
            }

            return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var valueAsString = (string)context.Object;
            if (string.IsNullOrEmpty(valueAsString))
            {
                return Task.CompletedTask;
            }

            var response = context.HttpContext.Response;
            return response.WriteAsync(valueAsString, encoding);
        }
    }
}
