// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public class SignOutContext
    {
        public SignOutContext(string authenticationScheme, IDictionary<string, string> properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            AuthenticationScheme = authenticationScheme;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public string AuthenticationScheme { get; }

        public IDictionary<string, string> Properties { get; }

        public bool Accepted { get; private set; }

        public void Accept()
        {
            Accepted = true;
        }
    }
}