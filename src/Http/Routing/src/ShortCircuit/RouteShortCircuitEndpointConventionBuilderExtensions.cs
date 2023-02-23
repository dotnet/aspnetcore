// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.ShortCircuit;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Short Circuit extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RouteShortCircuitEndpointConventionBuilderExtensions
{
    private static readonly ShortCircuitMetadata _nullShortCircuitMetadata = new ShortCircuitMetadata(null);

    /// <summary>
    /// Short Circuit the the endpoint(s).
    /// The execution of the endpoint will happen in UseRouting middleware instead of UseEndpoint.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="statusCode">The status code to set in the response.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static IEndpointConventionBuilder ShortCircuit(this IEndpointConventionBuilder builder, int? statusCode = null)
    {
        if (statusCode is null)
        {
            builder.Add(b => b.Metadata.Add(_nullShortCircuitMetadata));
        }
        else
        {
            builder.Add(b => b.Metadata.Add(new ShortCircuitMetadata(statusCode)));
        }

        return builder;
    }
}
