// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

[assembly: TypeForwardedTo(typeof(IEndpointFeature))]
[assembly: TypeForwardedTo(typeof(IRouteValuesFeature))]
[assembly: TypeForwardedTo(typeof(Endpoint))]
[assembly: TypeForwardedTo(typeof(EndpointMetadataCollection))]
[assembly: TypeForwardedTo(typeof(RouteValueDictionary))]
