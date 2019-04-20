// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace CodeGenerator
{
    public class TransportConnectionFeatureCollection
    {
        public static string GenerateFile()
        {
            // NOTE: This list MUST always match the set of feature interfaces implemented by TransportConnection.
            // See also: src/Kestrel.Transport.Abstractions/Internal/TransportConnection.FeatureCollection.cs
            var implementedFeatures = new[]
            {
                "IHttpConnectionFeature",
                "IConnectionIdFeature",
                "IConnectionTransportFeature",
                "IConnectionItemsFeature",
                "IMemoryPoolFeature",
                "IApplicationTransportFeature",
                "ITransportSchedulerFeature",
                "IConnectionLifetimeFeature",
                "IConnectionHeartbeatFeature",
                "IConnectionLifetimeNotificationFeature"
            };

            var extraFeatures = new[]
            {
                "ITcpCorkFeature"
            };

            var usings = $@"
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;";

            return FeatureCollectionGenerator.GenerateFile(
                namespaceName: "Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal",
                className: "TransportConnection",
                allFeatures: implementedFeatures.Concat(extraFeatures).ToArray(),
                implementedFeatures: implementedFeatures,
                extraUsings: usings,
                fallbackFeatures: null);
        }
    }
}
