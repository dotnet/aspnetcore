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
    /// <param name="requestBodyLogLimit">Sets the <see cref="HttpLoggingOptions.RequestBodyLogLimit"/> for this endpoint. A value of <c>-1</c> means use the default setting in <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>.</param>
    /// <param name="responseBodyLogLimit">Sets the <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/> for this endpoint. A value of <c>-1</c> means use the default setting in <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>.</param>
    /// <returns>The original convention builder parameter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="requestBodyLogLimit"/> or <paramref name="responseBodyLogLimit"/> is less than <c>0</c>.</exception>
    public static TBuilder WithHttpLogging<TBuilder>(this TBuilder builder, HttpLoggingFields loggingFields, int? requestBodyLogLimit = null, int? responseBodyLogLimit = null) where TBuilder : IEndpointConventionBuilder
    {
        // Construct outside build.Add lambda to allow exceptions to be thrown immediately
        var metadata = new HttpLoggingAttribute(loggingFields);

        if (requestBodyLogLimit is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(requestBodyLogLimit.Value, 0, nameof(requestBodyLogLimit));
            metadata.RequestBodyLogLimit = requestBodyLogLimit.Value;
        }
        if (responseBodyLogLimit is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(responseBodyLogLimit.Value, 0, nameof(responseBodyLogLimit));
            metadata.ResponseBodyLogLimit = responseBodyLogLimit.Value;
        }

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(metadata);
        });
        return builder;
    }
}
