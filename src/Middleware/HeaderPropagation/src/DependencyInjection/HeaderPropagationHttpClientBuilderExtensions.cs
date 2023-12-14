// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IHttpClientBuilder"/> extension methods for <see cref="HeaderPropagationMiddleware"/> which propagates request headers to an <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public static class HeaderPropagationHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request.
    /// </summary>
    /// <remarks>
    /// When using this method, all the configured headers will be applied to the outgoing HTTP requests.
    /// </remarks>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
    public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
    /// <param name="configure">A delegate used to configure the <see cref="HeaderPropagationMessageHandlerOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
    public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder, Action<HeaderPropagationMessageHandlerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddHeaderPropagation();

        builder.AddHttpMessageHandler(services =>
        {
            var options = new HeaderPropagationMessageHandlerOptions();
            configure(options);
            return new HeaderPropagationMessageHandler(options, services.GetRequiredService<HeaderPropagationValues>());
        });

        return builder;
    }
}
