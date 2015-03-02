// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Http.Interfaces.Authentication
{
    public interface IHttpAuthenticationFeature
    {
        ClaimsPrincipal User { get; set; }
        IAuthenticationHandler Handler { get; set; }
    }
}