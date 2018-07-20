// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    public sealed class EndpointFeature : IEndpointFeature, IRoutingFeature
    {
        private RouteData _routeData;
        private RouteValueDictionary _values;

        public Endpoint Endpoint { get; set; }

        public Func<RequestDelegate, RequestDelegate> Invoker { get; set; }

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

        RouteData IRoutingFeature.RouteData
        {
            get
            {
                if (_routeData == null)
                {
                    _routeData = new RouteData(_values);
                }

                return _routeData;
            }
            set => throw new NotSupportedException();
        }
    }
}
