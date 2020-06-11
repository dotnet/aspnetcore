// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class TestAuthHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var req in context.Requirements)
            {
                context.Succeed(req);
            }

            var hasClaim = context.User.HasClaim(o => o.Type == ClaimTypes.NameIdentifier && !string.IsNullOrEmpty(o.Value));

            if (!hasClaim)
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
