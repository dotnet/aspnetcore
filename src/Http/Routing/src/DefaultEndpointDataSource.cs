// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Provides a collection of <see cref="Endpoint"/> instances.
    /// </summary>
    public sealed class DefaultEndpointDataSource : EndpointDataSource
    {
        private readonly IReadOnlyList<Endpoint> _endpoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEndpointDataSource" /> class.
        /// </summary>
        /// <param name="endpoints">The <see cref="Endpoint"/> instances that the data source will return.</param>
        public DefaultEndpointDataSource(params Endpoint[] endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            _endpoints = (Endpoint[])endpoints.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEndpointDataSource" /> class.
        /// </summary>
        /// <param name="endpoints">The <see cref="Endpoint"/> instances that the data source will return.</param>
        public DefaultEndpointDataSource(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            _endpoints = new List<Endpoint>(endpoints);
        }

        /// <summary>
        /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Endpoint"/>
        /// instances.
        /// </summary>
        /// <returns>The <see cref="IChangeToken"/>.</returns>
        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

        /// <summary>
        /// Returns a read-only collection of <see cref="Endpoint"/> instances.
        /// </summary>
        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;
    }
}
