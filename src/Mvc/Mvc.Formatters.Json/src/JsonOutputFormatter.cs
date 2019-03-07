// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> for JSON content.
    /// </summary>
    public sealed class JsonOutputFormatter : TextOutputFormatter
    {
        private readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new <see cref="JsonOutputFormatter"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="JsonFormatterOptions"/>.</param>
        public JsonOutputFormatter(JsonFormatterOptions options)
        {
            _serializerOptions = options?.SerializerOptions ?? throw new ArgumentNullException(nameof(options));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <inheritdoc />
        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var httpContext = context.HttpContext;
            var requestAborted = httpContext.RequestAborted;

            var writeStream = GetWriteStream(httpContext, selectedEncoding);
            await JsonSerializer.WriteAsync(context.Object, context.ObjectType, writeStream, _serializerOptions, requestAborted);
            await writeStream.FlushAsync(requestAborted);
        }

        private Stream GetWriteStream(HttpContext httpContext, Encoding selectedEncoding)
        {
            if (selectedEncoding == Encoding.UTF8)
            {
                return httpContext.Response.Body;
            }

            return new TranscodingWriteStream(httpContext.Response.Body, selectedEncoding);
        }
    }
}
