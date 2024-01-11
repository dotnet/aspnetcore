// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Short circuit extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RouteShortCircuitEndpointConventionBuilderExtensions
{
    private static readonly ShortCircuitAttribute _200ShortCircuitMetadata = new(200);
    private static readonly ShortCircuitAttribute _401ShortCircuitMetadata = new(401);
    private static readonly ShortCircuitAttribute _404ShortCircuitMetadata = new(404);
    private static readonly ShortCircuitAttribute _nullShortCircuitMetadata = new(null);

    /// <summary>
    /// Short circuit the endpoint(s).
    /// The execution of the endpoint will happen in UseRouting middleware instead of UseEndpoint.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="statusCode">The status code to set in the response.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static IEndpointConventionBuilder ShortCircuit(this IEndpointConventionBuilder builder, int? statusCode = null)
    {
        var metadata = statusCode switch
        {
            200 => _200ShortCircuitMetadata,
            401 => _401ShortCircuitMetadata,
            404 => _404ShortCircuitMetadata,
            null => _nullShortCircuitMetadata,
            _ => new ShortCircuitAttribute(statusCode)
        };

        builder.Add(b => b.Metadata.Add(metadata));
        return builder;
    }
}
