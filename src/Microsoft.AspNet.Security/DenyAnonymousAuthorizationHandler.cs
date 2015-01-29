// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    public class DenyAnonymousAuthorizationHandler : AuthorizationHandler<DenyAnonymousAuthorizationRequirement>
    {
        public override Task<bool> CheckAsync(AuthorizationContext context, DenyAnonymousAuthorizationRequirement requirement)
        {
            var user = context.User;
            var userIsAnonymous =
                user == null ||
                user.Identity == null ||
                !user.Identity.IsAuthenticated;
            return Task.FromResult(!userIsAnonymous);
        }
    }
}
