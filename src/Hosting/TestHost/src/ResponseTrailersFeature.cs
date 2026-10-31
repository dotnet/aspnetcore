// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost;

internal sealed class ResponseTrailersFeature : IHttpResponseTrailersFeature
{
    public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();
}
