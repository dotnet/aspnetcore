// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal enum RequestParameterSource
{
    Query,
    Route,
    Header,
    Form,
    Service,
    BodyOrService
}

internal sealed class RequestParameter : IEquatable<RequestParameter>
{
    public string Name { get; }
    public string Type { get; }
    public RequestParameterSource Source { get; set; }
    public bool IsOptional { get; set; }
    public object? DefaultValue { get; set; }

    public override bool Equals(object? obj)
        => obj is RequestParameter requestParameter && Equals(requestParameter);

    public bool Equals(RequestParameter other)
        => Name.Equals(other.Name, StringComparison.Ordinal) &&
            Type.Equals(other.Type, StringComparison.Ordinal) &&
            Source == other.Source &&
            IsOptional == other.IsOptional &&
            DefaultValue.Equals(DefaultValue);

    public override int GetHashCode()
        => (Name, Type, Source, IsOptional, DefaultValue).GetHashCode();
}

internal sealed class EndpointRoute : IEquatable<EndpointRoute>
{
    public string RoutePattern { get; set; }

    public List<string> RouteParameters { get; set; }

    public override bool Equals(object? obj)
        => obj is EndpointRoute route && Equals(route);

    public bool Equals(EndpointRoute other)
        => RoutePattern.Equals(other.RoutePattern, StringComparison.Ordinal) &&
            RouteParameters.Equals(other.RouteParameters);

    public override int GetHashCode()
        => (RoutePattern, RouteParameters).GetHashCode();
}

internal sealed class EndpointResponse : IEquatable<EndpointResponse>
{
    public string ResponseType { get; set; }
    public string ContentType { get; set; }
    public override bool Equals(object? obj)
        => obj is EndpointResponse endpointResponse && Equals(endpointResponse);

    public bool Equals(EndpointResponse other)
        => ResponseType == other.ResponseType
            && ContentType == other.ContentType;

    public override int GetHashCode()
        => (ResponseType, ContentType).GetHashCode();
}

internal sealed class EndpointRequest : IEquatable<EndpointRequest>
{
    public List<RequestParameter> RequestParameters { get; set; }

    public override bool Equals(object? obj)
        => obj is EndpointRequest endpointRequest && Equals(endpointRequest);

    public bool Equals(EndpointRequest other)
        => RequestParameters == other.RequestParameters;

    public override int GetHashCode()
        => RequestParameters.GetHashCode();
}

internal sealed class Endpoint : IEquatable<Endpoint>
{
    public string HttpMethod { get; set; }
    public EndpointRoute Route { get; set; }
    public EndpointRequest Request { get; set; }
    public EndpointResponse Response { get; set; }
    public (string, int) Location { get; set; }

    public override bool Equals(object? obj)
        => obj is Endpoint endpoint && Equals(endpoint);

    public bool Equals(Endpoint other)
        => HttpMethod == other.HttpMethod &&
            Route.Equals(other.Route) &&
            Request.Equals(other.Request) &&
            Response.Equals(other.Response) &&
            Location.Equals(other.Location);

    public override int GetHashCode()
        => (HttpMethod, Route, Request, Response, Location).GetHashCode();
}
