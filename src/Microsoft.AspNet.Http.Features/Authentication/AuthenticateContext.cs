// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Http.Features.Authentication
{
    public class AuthenticateContext
    {
        public AuthenticateContext(string authenticationScheme)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            AuthenticationScheme = authenticationScheme;
        }

        public string AuthenticationScheme { get; }

        public bool Accepted { get; private set; }

        public ClaimsPrincipal Principal { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public IDictionary<string, object> Description { get; private set; }

        public Exception Error { get; private set; }

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

        public virtual void Failed(Exception error)
        {
            Error = error;
            Accepted = true;
        }
    }
}
