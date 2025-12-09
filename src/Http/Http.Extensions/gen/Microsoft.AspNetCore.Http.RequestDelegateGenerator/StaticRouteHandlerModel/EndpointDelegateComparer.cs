// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal sealed class EndpointDelegateComparer : IEqualityComparer<Endpoint>
{
    public static readonly EndpointDelegateComparer Instance = new EndpointDelegateComparer();

    public bool Equals(Endpoint a, Endpoint b) => Endpoint.SignatureEquals(a, b);
    public int GetHashCode(Endpoint endpoint) => Endpoint.GetSignatureHashCode(endpoint);
}
