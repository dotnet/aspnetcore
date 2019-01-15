// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding routing metadata to endpoint instances using <see cref="IEndpointConventionBuilder"/>.
    /// </summary>
    public static class RoutingEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Requires that endpoints match one of the specified hosts during routing.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add the metadata to.</param>
        /// <param name="hosts">
        /// The hosts used during routing.
        /// Hosts should be Unicode rather than punycode, and may have a port.
        /// An empty collection means any host will be accepted.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IEndpointConventionBuilder RequireHost(this IEndpointConventionBuilder builder, params string[] hosts)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (hosts == null)
            {
                throw new ArgumentNullException(nameof(hosts));
            }

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new HostAttribute(hosts));
            });
            return builder;
        }
    }
}
