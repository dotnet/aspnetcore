// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal sealed class EndpointShapeComparer : IEqualityComparer<Endpoint>, IComparer<Endpoint>
{
    public static readonly EndpointShapeComparer Instance = new EndpointShapeComparer();

    public bool Equals(Endpoint a, Endpoint b) => Compare(a, b) == 0;

    public int GetHashCode(Endpoint endpoint) => HashCode.Combine(
        endpoint.Response.WrappedResponseType,
        endpoint.Response.IsVoid,
        endpoint.Response.IsAwaitable,
        endpoint.HttpMethod);

    public int Compare(Endpoint a, Endpoint b)
    {
        if (a.Response.IsAwaitable == b.Response.IsAwaitable &&
            a.Response.IsVoid == b.Response.IsVoid &&
            a.Response.WrappedResponseType.Equals(b.Response.WrappedResponseType, StringComparison.Ordinal) &&
            a.HttpMethod.Equals(b.HttpMethod, StringComparison.Ordinal))
        {
            return 0;
        }
        return -1;
    }
}
