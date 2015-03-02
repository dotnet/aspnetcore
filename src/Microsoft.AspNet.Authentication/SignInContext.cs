// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication
{
    public class SignInContext
    {
        public SignInContext(ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            Principal = principal;
            Properties = properties;
        }

        public ClaimsPrincipal Principal { get; private set; }
        public AuthenticationProperties Properties { get; private set; }
    }
}
