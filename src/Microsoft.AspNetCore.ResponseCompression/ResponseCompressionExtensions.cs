// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the ResponseCompression middleware.
    /// </summary>
    public static class ResponseCompressionExtensions
    {
        /// <summary>
        /// Allows to compress HTTP Responses.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <param name="options">The <see cref="ResponseCompressionOptions"/>.</param>
        public static IApplicationBuilder UseResponseCompression(this IApplicationBuilder builder, ResponseCompressionOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<ResponseCompressionMiddleware>(Options.Create(options));
        }
    }
}
