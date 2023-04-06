// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpLogging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// HttpLogging middleware extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class HttpLoggingEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds endpoint specific settings for the HttpLogging middleware.
    /// </summary>
    /// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="loggingFields">The <see cref="HttpLoggingFields"/> to apply to this endpoint.</param>
    /// <param name="requestBodyLogLimit">Sets the <see cref="HttpLoggingOptions.RequestBodyLogLimit"/> for this endpoint.</param>
    /// <param name="responseBodyLogLimit">Sets the <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/> for this endpoint.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder WithHttpLogging<TBuilder>(this TBuilder builder, HttpLoggingFields loggingFields, int? requestBodyLogLimit = null, int? responseBodyLogLimit = null) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new HttpLoggingAttribute(loggingFields, requestBodyLogLimit ?? -1, responseBodyLogLimit ?? -1));
        });
        return builder;
    }
}
