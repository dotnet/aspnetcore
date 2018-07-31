// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing
{
    internal static class EndpointFactory
    {
        public static MatcherEndpoint CreateMatcherEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            object requiredValues = null,
            int order = 0,
            string displayName = null,
            params object[] metadata)
        {
            var metadataCollection = EndpointMetadataCollection.Empty;
            if (metadata != null)
            {
                metadataCollection = new EndpointMetadataCollection(metadata);
            }

            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template, defaults, constraints),
                new RouteValueDictionary(requiredValues),
                order,
                metadataCollection,
                displayName);
        }
    }
}
