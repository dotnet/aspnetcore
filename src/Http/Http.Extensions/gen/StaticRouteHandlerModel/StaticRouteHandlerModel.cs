// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.SourceGeneration.StaticRouteHandlerModel;

internal enum RequestParameterSource
{
    Query,
    Route,
    Header,
    Form,
    Service,
    QueryOrService
}

internal sealed class RequestParameter
{
    public string Name { get; }
    public string Type { get; }
    public RequestParameterSource Source { get; set; }
    public bool IsOptional { get; set; }
    public object? DefaultValue { get; set; }
}

internal sealed class EndpointRoute
{
    public string RoutePattern { get; set; }

    public List<string> RouteParameters { get; set; }
}

internal sealed class EndpointResponse
{
    public string ResponseType { get; set; }
    public string ContentType { get; set; }
}

internal sealed class EndpointRequest
{
    public List<RequestParameter> RequestParameters { get; set; }
}

internal sealed class Endpoint
{
    public string HttpMethod { get; set; }
    public EndpointRoute Route { get; set; }
    public EndpointRequest Request { get; set; }
    public EndpointResponse Response { get; set; }
    public (string, int) Location { get; set; }
}
