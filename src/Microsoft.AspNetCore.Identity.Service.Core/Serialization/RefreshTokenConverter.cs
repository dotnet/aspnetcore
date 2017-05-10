// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service.Serialization
{
    internal class RefreshTokenConverter : TokenConverter<RefreshToken>
    {
        public override RefreshToken CreateToken(IEnumerable<Claim> claims)
        {
            return new RefreshToken(claims);
        }
    }
}
