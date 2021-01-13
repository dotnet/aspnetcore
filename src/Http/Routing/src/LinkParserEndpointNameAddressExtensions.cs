// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Extension methods for using <see cref="LinkParser"/> with an endpoint name.
    /// </summary>
    public static class LinkParserEndpointNameAddressExtensions
    {
        /// <summary>
        /// Attempts to parse the provided <paramref name="path"/> using the route pattern
        /// specified by the <see cref="Endpoint"/> matching <paramref name="endpointName"/>.
        /// </summary>
        /// <param name="parser">The <see cref="LinkParser"/>.</param>
        /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
        /// <param name="path">The URI path to parse.</param>
        /// <returns>
        /// A <see cref="RouteValueDictionary"/> with the parsed values if parsing is successful; 
        /// otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="ParsePathByEndpointName(LinkParser, string, PathString)"/> will attempt to first resolve
        /// <see cref="Endpoint"/> instances that match <paramref name="endpointName"/> and then use the route
        /// pattern associated with each endpoint to parse the URL path. 
        /// </para>
        /// <para>
        /// The parsing operation will fail and return <c>null</c> if either no endpoints are found or none
        /// of the route patterns match the provided URI path.
        /// </para>
        /// </remarks>
        public static RouteValueDictionary ParsePathByEndpointName(
            this LinkParser parser,
            string endpointName,
            PathString path)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            return parser.ParsePathByAddress<string>(endpointName, path);
        }
    }
}
