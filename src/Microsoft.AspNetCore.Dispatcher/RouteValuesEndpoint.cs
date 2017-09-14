// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RouteValuesEndpoint : Endpoint
    {
        public RouteValuesEndpoint(string displayName)
        {
            DisplayName = displayName;
        }

        public override string DisplayName { get; }

        public RequestDelegate RequestDelegate { get; set; }

        public RouteValueDictionary RequiredValues { get; set; }
    }
}
