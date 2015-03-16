// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class AuthenticateContext : IAuthenticateContext
    {
        private AuthenticationResult _result;
        private bool _accepted;

        public AuthenticateContext([NotNull] string authenticationScheme)
        {
            AuthenticationScheme = authenticationScheme;
        }

        public string AuthenticationScheme { get; private set; }

        public AuthenticationResult Result { get; set; }

        public bool Accepted
        {
            get { return _accepted; }
        }

        public void Authenticated(ClaimsPrincipal principal, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            var descrip = new AuthenticationDescription(description);
            _accepted = true;
            Result = new AuthenticationResult(principal, new AuthenticationProperties(properties), descrip);
        }

        public void NotAuthenticated()
        {
            _accepted = true;
        }
    }
}
