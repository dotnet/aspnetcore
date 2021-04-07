// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace CodeGenerator
{
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
                "IMemoryPoolFeature",
                "IConnectionLifetimeFeature",
                "IConnectionSocketFeature"
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
                fallbackFeatures: null);
        }
    }
}
