// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class ChallengeContext : IChallengeContext
    {
        private bool _accepted;

        public ChallengeContext(string authenticationScheme, IDictionary<string, string> properties)
        {
            AuthenticationScheme = authenticationScheme;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);

            // The default Challenge with no scheme is always accepted
            _accepted = string.IsNullOrEmpty(authenticationScheme);
        }

        public string AuthenticationScheme { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public bool Accepted
        {
            get { return _accepted; }
        }

        public void Accept()
        {
            _accepted = true;
        }
    }
}
