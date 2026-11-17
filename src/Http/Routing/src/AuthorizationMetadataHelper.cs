// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal static class AuthorizationMetadataHelper
{
    // This check should be kept in sync with the policy computation in AuthorizationMiddleware.
    public static bool HasAuthorizationMetadata(Endpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null
        || endpoint.Metadata.GetMetadata<AuthorizationPolicy>() is not null
        || endpoint.Metadata.GetMetadata<IAuthorizationRequirementData>() is not null;
}
