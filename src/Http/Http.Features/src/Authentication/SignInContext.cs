// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public class SignInContext
    {
        public SignInContext(string authenticationScheme, ClaimsPrincipal principal, IDictionary<string, string> properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            AuthenticationScheme = authenticationScheme;
            Principal = principal;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public string AuthenticationScheme { get; }

        public ClaimsPrincipal Principal { get; }

        public IDictionary<string, string> Properties { get; }

        public bool Accepted { get; private set; }

        public void Accept()
        {
            Accepted = true;
        }
    }
}