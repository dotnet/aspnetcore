// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitJSComponentConfiguration : IJSComponentConfiguration
    {
        public JSComponentConfigurationStore JSComponents { get; } = new();

        public void AddToEndpointMetadata(IEndpointConventionBuilder conventionBuilder)
        {
            conventionBuilder.Add(endpointBuilder => endpointBuilder.Metadata.Add(this));
        }

        public static CircuitJSComponentConfiguration? GetFromEndpointMetadata(Endpoint endpoint)
        {
            return endpoint.Metadata.OfType<CircuitJSComponentConfiguration>().FirstOrDefault();
        }
    }
}
