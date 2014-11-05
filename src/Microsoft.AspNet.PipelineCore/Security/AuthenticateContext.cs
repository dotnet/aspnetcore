// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class AuthenticateContext : IAuthenticateContext
    {
        private List<AuthenticationResult> _results;
        private List<string> _accepted;

        public AuthenticateContext([NotNull] IEnumerable<string> authenticationTypes)
        {
            AuthenticationTypes = authenticationTypes;
            _results = new List<AuthenticationResult>();
            _accepted = new List<string>();
        }

        public IEnumerable<string> AuthenticationTypes { get; private set; }

        public IEnumerable<AuthenticationResult> Results
        {
            get { return _results; }
        }

        public IEnumerable<string> Accepted
        {
            get { return _accepted; }
        }

        public void Authenticated(ClaimsIdentity identity, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            var descrip = new AuthenticationDescription(description);
            _accepted.Add(descrip.AuthenticationType); // may not match identity.AuthType
            _results.Add(new AuthenticationResult(identity, new AuthenticationProperties(properties), descrip));
        }

        public void NotAuthenticated(string authenticationType, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            _accepted.Add(authenticationType);
        }
    }
}
