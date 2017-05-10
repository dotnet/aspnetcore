// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service.Serialization
{
    internal class AuthorizationCodeConverter : TokenConverter<AuthorizationCode>
    {
        public override AuthorizationCode CreateToken(IEnumerable<Claim> claims)
        {
            return new AuthorizationCode(claims);
        }
    }
}
