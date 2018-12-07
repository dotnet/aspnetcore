// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing
{
    public sealed class EndpointSelectorContext : IEndpointFeature, IRouteValuesFeature, IRoutingFeature
    {
        private RouteData _routeData;
        private RouteValueDictionary _routeValues;

        /// <summary>
        /// Gets or sets the selected <see cref="Http.Endpoint"/> for the current
        /// request.
        /// </summary>
        public Endpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
        /// request.
        /// </summary>
        public RouteValueDictionary RouteValues
        {
            get => _routeValues;
            set
            {
                _routeValues = value;

                // RouteData will be created next get with new Values
                _routeData = null;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="RouteData"/> for the current request.
        /// </summary>
        /// <remarks>
        /// The setter is not implemented. Use <see cref="RouteValues"/> to set the route values.
        /// </remarks>
        RouteData IRoutingFeature.RouteData
        {
            get
            {
                if (_routeData == null)
                {
                    _routeData = _routeValues == null ? new RouteData() : new RouteData(_routeValues);

                    // Note: DataTokens won't update if someone else overwrites the Endpoint
                    // after route values has been set. This seems find since endpoints are a new
                    // feature and DataTokens are for back-compat.
                    var dataTokensMetadata = Endpoint?.Metadata.GetMetadata<IDataTokensMetadata>();
                    if (dataTokensMetadata != null)
                    {
                        var dataTokens = _routeData.DataTokens;
                        foreach (var kvp in dataTokensMetadata.DataTokens)
                        {
                            _routeData.DataTokens.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                return _routeData;
            }
            set => throw new NotSupportedException();
        }
    }
}