// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Http.Features.Authentication
{
    public interface IHttpAuthenticationFeature
    {
        ClaimsPrincipal User { get; set; }

        IAuthenticationHandler Handler { get; set; }
    }
}