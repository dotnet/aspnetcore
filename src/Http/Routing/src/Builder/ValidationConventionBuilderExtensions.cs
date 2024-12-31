// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for managing model validation on an endpoint.
/// </summary>
public static class ValidationConventionBuilderExtensions
{
    /// <summary>
    /// Adds validation to the specified <see cref="IEndpointConventionBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add validation to.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> with validation added.</returns>
    public static IEndpointConventionBuilder WithValidation(this IEndpointConventionBuilder builder)
    {
        // Intentionally empty, requires interception by validations generator
        return builder;
    }
}
