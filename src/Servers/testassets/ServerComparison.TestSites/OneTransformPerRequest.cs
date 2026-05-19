// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ServerComparison.TestSites;

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
