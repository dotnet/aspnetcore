// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IEndpointConventionBuilder"/> to interact with
/// parameter validation features.
/// </summary>
public static class ValidationEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Disables validation for the specified endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>
    /// The <see cref="IEndpointConventionBuilder"/> for chaining.
    /// </returns>
    public static TBuilder DisableValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new DisableValidationMetadata());
        return builder;
    }

    private sealed class DisableValidationMetadata : IDisableValidationMetadata
    {
    }
}
