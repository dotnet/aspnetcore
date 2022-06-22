// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CodeGenerator;

public class TransportMultiplexedConnectionFeatureCollection
{
    public static string GenerateFile()
    {
        // NOTE: This list MUST always match the set of feature interfaces implemented by TransportConnectionBase.
        // See also: shared/TransportConnectionBase.FeatureCollection.cs
        var allFeatures = new[]
        {
                "IConnectionIdFeature",
                "IConnectionTransportFeature",
                "IConnectionItemsFeature",
                "IMemoryPoolFeature",
                "IConnectionLifetimeFeature",
                "IProtocolErrorCodeFeature",
                "ITlsConnectionFeature"
            };
        var implementedFeatures = new[]
        {
                "IConnectionIdFeature",
                "IConnectionItemsFeature",
                "IMemoryPoolFeature",
                "IConnectionLifetimeFeature"
            };

        var usings = $@"
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;";

        return FeatureCollectionGenerator.GenerateFile(
            namespaceName: "Microsoft.AspNetCore.Connections",
            className: "TransportMultiplexedConnection",
            allFeatures: allFeatures,
            implementedFeatures: implementedFeatures,
            extraUsings: usings,
            fallbackFeatures: null);
    }
}
