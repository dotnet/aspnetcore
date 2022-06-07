// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

public interface IProblemDetailsMapPolicy
{
    bool CanMap(HttpContext context, EndpointMetadataCollection? metadata, int? statusCode, bool isRouting);
}
