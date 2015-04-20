// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Authentication
{
    public class AuthenticateContext
    {
        public AuthenticateContext([NotNull] string authenticationScheme)
        {
            AuthenticationScheme = authenticationScheme;
        }

        public string AuthenticationScheme { get; }

        public bool Accepted { get; private set; }

        public ClaimsPrincipal Principal { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public IDictionary<string, object> Description { get; private set; }

        public virtual void Authenticated(ClaimsPrincipal principal, IDictionary<string, string> properties, IDictionary<string, object> description)
        {
            Accepted = true;
            Principal = principal;
            Properties = properties;
            Description = description;
        }

        public virtual void NotAuthenticated()
        {
            Accepted = true;
        }
    }
}
