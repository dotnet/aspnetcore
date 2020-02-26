// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace AuthSamples.ClaimsTransformer
{
    public class ClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // This will run every time Authenticate is called so its better to create a new Principal
            var transformed = new ClaimsPrincipal();
            transformed.AddIdentities(principal.Identities);
            transformed.AddIdentity(new ClaimsIdentity(new Claim[] 
            {
                new Claim("Transformed", DateTime.Now.ToString())
            }));
            return Task.FromResult(transformed);
        }
    }
}
