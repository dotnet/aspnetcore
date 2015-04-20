// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Authentication
{
    public class SignInContext
    {
        public SignInContext([NotNull] string authenticationScheme, [NotNull] ClaimsPrincipal principal, IDictionary<string, string> properties)
        {
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