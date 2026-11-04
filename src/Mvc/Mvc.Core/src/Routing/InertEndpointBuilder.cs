// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class InertEndpointBuilder : EndpointBuilder
{
    public override Endpoint Build()
    {
        return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
    }
}
