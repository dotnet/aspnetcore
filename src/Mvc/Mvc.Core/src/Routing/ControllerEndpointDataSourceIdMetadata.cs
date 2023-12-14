// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class ControllerEndpointDataSourceIdMetadata
{
    public ControllerEndpointDataSourceIdMetadata(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
