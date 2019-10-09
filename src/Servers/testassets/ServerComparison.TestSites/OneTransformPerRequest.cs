// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ServerComparison.TestSites
{
    public class OneTransformPerRequest : IClaimsTransformation
    {
        public OneTransformPerRequest(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        public IHttpContextAccessor ContextAccessor { get; }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var context = ContextAccessor.HttpContext;
            if (context.Items["Transformed"] != null)
            {
                throw new InvalidOperationException("Transformation ran multiple times.");
            }
            context.Items["Transformed"] = true;
            return Task.FromResult(principal);
        }
    }
}
