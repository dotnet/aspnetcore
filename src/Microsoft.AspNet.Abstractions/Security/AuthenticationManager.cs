// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions.Security
{
    public abstract class AuthenticationManager
    {
        public abstract HttpContext HttpContext { get; }

        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationTypes();
        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationTypes(Func<AuthenticationDescription, bool> predicate);

        public abstract AuthenticationResult Authenticate(string authenticationType); // TODO: Is sync a good idea?
        public abstract IEnumerable<AuthenticationResult> Authenticate(IList<string> authenticationTypes);

        public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationType);
        public abstract Task<IEnumerable<AuthenticationResult>> AuthenticateAsync(IList<string> authenticationTypes);

        public abstract void Challenge();
        public abstract void Challenge(AuthenticationProperties properties);
        public abstract void Challenge(string authenticationType);
        public abstract void Challenge(string authenticationType, AuthenticationProperties properties);
        public abstract void Challenge(IList<string> authenticationTypes);
        public abstract void Challenge(IList<string> authenticationTypes, AuthenticationProperties properties);

        public abstract void SignIn(ClaimsPrincipal user); // TODO: This took multiple identities in Katana. Is that needed?
        public abstract void SignIn(ClaimsPrincipal user, AuthenticationProperties properties); // TODO: ClaimsIdentity vs ClaimsPrincipal?

        public abstract void SignOut();
        public abstract void SignOut(string authenticationType);
        public abstract void SignOut(IList<string> authenticationTypes);
    }
}
