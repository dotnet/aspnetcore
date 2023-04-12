// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CodeGenerator;

public class TransportConnectionFeatureCollection
{
    public static string GenerateFile()
    {
        // NOTE: This list MUST always match the set of feature interfaces implemented by TransportConnection.
        // See also: shared/TransportConnection.FeatureCollection.cs

        var allFeatures = new[]
        {
            "IConnectionIdFeature",
            "IConnectionTransportFeature",
            "IConnectionItemsFeature",
            "IPersistentStateFeature",
            "IMemoryPoolFeature",
            "IConnectionLifetimeFeature",
            "IConnectionSocketFeature",
            "IProtocolErrorCodeFeature",
            "IStreamDirectionFeature",
            "IStreamIdFeature",
            "IStreamAbortFeature",
            "IStreamClosedFeature",
            "IConnectionMetricsTagsFeature"
        };

        var implementedFeatures = new[]
        {
            "IConnectionIdFeature",
            "IConnectionTransportFeature",
            "IConnectionItemsFeature",
            "IMemoryPoolFeature",
            "IConnectionLifetimeFeature"
        };

        var usings = $@"
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;";

        return FeatureCollectionGenerator.GenerateFile(
            namespaceName: "Microsoft.AspNetCore.Connections",
            className: "TransportConnection",
            allFeatures: allFeatures,
            implementedFeatures: implementedFeatures,
            extraUsings: usings,
            fallbackFeatures: "MultiplexedConnectionFeatures");
    }
}
