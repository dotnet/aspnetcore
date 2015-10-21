// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Authorization.Infrastructure
{
    public class DelegateRequirement : AuthorizationHandler<DelegateRequirement>, IAuthorizationRequirement
    {
        public Action<AuthorizationContext, DelegateRequirement> Handler { get; }

        public DelegateRequirement(Action<AuthorizationContext, DelegateRequirement> handleMe)
        {
            Handler = handleMe;
        }

        protected override void Handle(AuthorizationContext context, DelegateRequirement requirement)
        {
            Handler(context, requirement);
        }
    }
}
