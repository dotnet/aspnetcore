// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageEndpointDataSourceIdMetadata
{
    public PageEndpointDataSourceIdMetadata(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
