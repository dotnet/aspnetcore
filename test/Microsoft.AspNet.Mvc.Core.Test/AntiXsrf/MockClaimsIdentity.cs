// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    // Convenient class for mocking a ClaimsIdentity instance given some
    // prefabricated Claim instances.
    internal sealed class MockClaimsIdentity : ClaimsIdentity
    {
        private readonly List<Claim> _claims = new List<Claim>();

        public void AddClaim(string claimType, string value)
        {
            _claims.Add(new Claim(claimType, value));
        }

        public override IEnumerable<Claim> Claims
        {
            get { return _claims; }
        }
    }
}