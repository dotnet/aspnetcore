// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Authentication
{
    public abstract class AuthenticationManager
    {
        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

        public abstract AuthenticationResult Authenticate(string authenticationScheme);

        public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme);

        public virtual void Challenge()
        {
            Challenge(properties: null, authenticationScheme: null);
        }

        public virtual void Challenge(AuthenticationProperties properties)
        {
            Challenge(properties, "");
        }

        public virtual void Challenge(string authenticationScheme)
        {
            Challenge(properties: null, authenticationScheme: authenticationScheme);
        }

        public abstract void Challenge(AuthenticationProperties properties, string authenticationScheme);

        public abstract void SignIn(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties = null);

        public virtual void SignOut()
        {
            SignOut(authenticationScheme: null, properties: null);
        }

        public abstract void SignOut(string authenticationScheme);

        public abstract void SignOut(string authenticationScheme, AuthenticationProperties properties);
    }
}
