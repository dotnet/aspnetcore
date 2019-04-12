// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HeaderPropagation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HeaderPropagationHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddHeaderPropagation();

            builder.AddHttpMessageHandler<HeaderPropagationMessageHandler>();

            return builder;
        }
    }
}
