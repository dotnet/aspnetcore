// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Always copies the stream to the response, regardless of requested content type.
    /// </summary>
    public class StreamOutputFormatter : IOutputFormatter
    {
        /// <inheritdoc />
        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Ignore the passed in content type, if the object is a Stream.
            if (context.Object is Stream)
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            using (var valueAsStream = ((Stream)context.Object))
            {
                var response = context.HttpContext.Response;

                if (context.ContentType != null)
                {
                    response.ContentType = context.ContentType.ToString();
                }

                await valueAsStream.CopyToAsync(response.Body);
            }
        }
    }
}
