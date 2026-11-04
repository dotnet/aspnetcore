// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal sealed class EndpointHttpMethodComparer : IEqualityComparer<Endpoint>
{
    public static readonly EndpointHttpMethodComparer Instance = new();
    private static readonly IEqualityComparer<string> OrdinalComparer = StringComparer.Ordinal;

    public bool Equals(Endpoint x, Endpoint y) => OrdinalComparer.Equals(x.HttpMethod, y.HttpMethod);

    public int GetHashCode(Endpoint obj) => OrdinalComparer.GetHashCode(obj.HttpMethod);
}
