// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal enum EndpointParameterSource
{
    Route,
    Query,
    // This should only be necessary if the route pattern is not statically analyzable
    RouteOrQuery,
    Header,
    JsonBody,
    JsonBodyOrService,
    FormBody,
    Service,
    // SpecialType refers to HttpContext, HttpRequest, CancellationToken, Stream, etc...
    // that are specially checked for in RequestDelegateFactory.CreateArgument()
    SpecialType,
    BindAsync,
    // Unknown should be temporary for development.
    Unknown,
}
