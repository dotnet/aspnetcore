// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// A default implementation of <see cref="IEndpointFeature"/> and <see cref="IRoutingFeature"/>.
    /// </summary>
    public sealed class EndpointFeature : IEndpointFeature, IRoutingFeature
    {
        private RouteData _routeData;
        private RouteValueDictionary _values;

        /// <summary>
        /// Gets or sets the selected <see cref="Routing.Endpoint"/> for the current
        /// request.
        /// </summary>
        public Endpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a delegate that can be used to invoke the current
        /// <see cref="Routing.Endpoint"/>.
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> Invoker { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
        /// request.
        /// </summary>
        public RouteValueDictionary Values
        {
            get => _values;
            set
            {
                _values = value;

                // RouteData will be created next get with new Values
                _routeData = null;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="RouteData"/> for the current request.
        /// </summary>
        /// <remarks>
        /// The setter is not implemented. Use <see cref="Values"/> to set the route values.
        /// </remarks>
        RouteData IRoutingFeature.RouteData
        {
            get
            {
                if (_routeData == null)
                {
                    _routeData = _values == null ? new RouteData() : new RouteData(_values);

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
