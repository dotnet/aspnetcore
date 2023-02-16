// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.ShortCircuit;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 
/// </summary>
public static class RouteShortCircuitEndpointConventionBuilderExtensions
{
    private static readonly ShortCircuitMetadata _nullShortCircuitMetadata = new ShortCircuitMetadata(null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="statusCode"></param>
    /// <returns></returns>
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
