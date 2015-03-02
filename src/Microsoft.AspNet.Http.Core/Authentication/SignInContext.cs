// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Http.Interfaces.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class SignInContext : ISignInContext
    {
        private bool _accepted;

        public SignInContext([NotNull] string authenticationScheme, [NotNull] ClaimsPrincipal principal, IDictionary<string, string> dictionary)
        {
            AuthenticationScheme = authenticationScheme;
            Principal = principal;
            Properties = dictionary ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public ClaimsPrincipal Principal { get; }

        public IDictionary<string, string> Properties { get; }

        public string AuthenticationScheme { get; }

        public bool Accepted
        {
            get { return _accepted; }
        }

        public void Accept(IDictionary<string, object> description)
        {
            _accepted = true;
        }
    }
}