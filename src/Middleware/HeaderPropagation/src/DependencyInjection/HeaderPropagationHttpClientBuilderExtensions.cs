// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HeaderPropagationHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request.
        /// </summary>
        /// <remarks>
        /// The headers will be named using the value specified in <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
        /// </remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddHeaderPropagation();

            builder.AddHttpMessageHandler(services =>
            {
                var options = new HeaderPropagationMessageHandlerOptions();
                var middlewareOptions = services.GetRequiredService<IOptions<HeaderPropagationOptions>>();
                for (var i = 0; i < middlewareOptions.Value.Headers.Count; i++)
                {
                    var header = middlewareOptions.Value.Headers[i];
                    options.Headers.Add(header.CapturedHeaderName, header.CapturedHeaderName);
                }
                return new HeaderPropagationMessageHandler(options, services.GetRequiredService<HeaderPropagationValues>());
            });

            return builder;
        }

        /// <summary>
        /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request,
        /// explicitly specifying which headers to propagate.
        /// </summary>
        /// <remarks>This also allows to redefine the name to use for a header in the outgoing request.</remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="HeaderPropagationMessageHandlerOptions"/>.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder, Action<HeaderPropagationMessageHandlerOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Services.AddHeaderPropagation();

            builder.AddHttpMessageHandler(services=>
            {
                var options = new HeaderPropagationMessageHandlerOptions();
                configureOptions(options);
                return new HeaderPropagationMessageHandler(options, services.GetRequiredService<HeaderPropagationValues>());
            });

            return builder;
        }
    }
}
