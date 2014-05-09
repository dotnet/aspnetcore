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
        public AuthenticateContext(IList<string> authenticationTypes)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException("authenticationType");
            }
            AuthenticationTypes = authenticationTypes;
            Results = new List<AuthenticationResult>();
            Accepted = new List<string>();
        }

        public IList<string> AuthenticationTypes { get; private set; }

        public IList<AuthenticationResult> Results { get; private set; }

        public IList<string> Accepted { get; private set; }

        public void Authenticated(ClaimsIdentity identity, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            var descrip = new AuthenticationDescription(description);
            Accepted.Add(descrip.AuthenticationType); // may not match identity.AuthType
            Results.Add(new AuthenticationResult(identity, new AuthenticationProperties(properties), descrip));
        }

        public void NotAuthenticated(string authenticationType, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            Accepted.Add(authenticationType);
        }
    }
}
