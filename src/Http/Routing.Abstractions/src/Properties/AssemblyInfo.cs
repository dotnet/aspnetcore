// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

[assembly: TypeForwardedTo(typeof(IEndpointFeature))]
[assembly: TypeForwardedTo(typeof(IRouteValuesFeature))]
[assembly: TypeForwardedTo(typeof(Endpoint))]
[assembly: TypeForwardedTo(typeof(EndpointMetadataCollection))]
#pragma warning disable RS0016
[assembly: TypeForwardedTo(typeof(RouteValueDictionary))]
#pragma warning restore RS0016

