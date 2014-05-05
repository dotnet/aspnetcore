// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
