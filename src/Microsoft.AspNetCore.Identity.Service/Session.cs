// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class Session
    {
        public Session(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            User = user;
            Application = application;
        }

        public ClaimsPrincipal User { get; }
        public ClaimsPrincipal Application { get; }
    }
}
