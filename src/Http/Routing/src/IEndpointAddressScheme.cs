// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract to find endpoints based on the provided address.
    /// </summary>
    /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
    public interface IEndpointAddressScheme<TAddress>
    {
        /// <summary>
        /// Finds endpoints based on the provided <paramref name="address"/>.
        /// </summary>
        /// <param name="address">The information used to look up endpoints.</param>
        /// <returns>A collection of <see cref="Endpoint"/>.</returns>
        IEnumerable<Endpoint> FindEndpoints(TAddress address);
    }
}
