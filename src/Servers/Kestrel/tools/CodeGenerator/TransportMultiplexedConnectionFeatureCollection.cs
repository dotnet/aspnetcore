// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace CodeGenerator
{
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
                "IConnectionLifetimeFeature"
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
                skipResetFeatures: Array.Empty<string>(),
                extraUsings: usings,
                fallbackFeatures: null);
        }
    }
}
