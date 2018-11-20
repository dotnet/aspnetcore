// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public interface IHttpAuthenticationFeature
    {
        ClaimsPrincipal User { get; set; }

        [Obsolete("This is obsolete and will be removed in a future version. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        IAuthenticationHandler Handler { get; set; }
    }
}