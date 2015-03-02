// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Http.Interfaces.Authentication;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class AuthenticateContext : IAuthenticateContext
    {
        private List<AuthenticationResult> _results;
        private List<string> _accepted;

        public AuthenticateContext([NotNull] IEnumerable<string> authenticationSchemes)
        {
            AuthenticationSchemes = authenticationSchemes;
            _results = new List<AuthenticationResult>();
            _accepted = new List<string>();
        }

        public IEnumerable<string> AuthenticationSchemes { get; private set; }

        public IEnumerable<AuthenticationResult> Results
        {
            get { return _results; }
        }

        public IEnumerable<string> Accepted
        {
            get { return _accepted; }
        }

        public void Authenticated(ClaimsPrincipal principal, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            var descrip = new AuthenticationDescription(description);
            _accepted.Add(descrip.AuthenticationScheme); // may not match identity.AuthType
            _results.Add(new AuthenticationResult(principal, new AuthenticationProperties(properties), descrip));
        }

        public void NotAuthenticated(string authenticationScheme, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            _accepted.Add(authenticationScheme);
        }
    }
}
