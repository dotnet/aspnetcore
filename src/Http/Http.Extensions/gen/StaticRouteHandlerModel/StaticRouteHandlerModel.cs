// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal enum RequestParameterSource
{
    Query,
    Route,
    Header,
    Form,
    Service,
    BodyOrService,
}

internal record RequestParameter(string Name, string Type, RequestParameterSource Source, bool IsOptional, object? DefaultValue);
internal record EndpointRoute(string RoutePattern);
internal record EndpointResponse(string ResponseType, string ContentType);
internal record Endpoint(string HttpMethod, EndpointRoute Route, EndpointResponse Response, (string, int) Location);
