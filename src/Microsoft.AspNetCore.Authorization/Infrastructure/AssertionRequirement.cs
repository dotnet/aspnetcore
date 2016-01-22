// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Authorization.Infrastructure
{
    public class AssertionRequirement : AuthorizationHandler<AssertionRequirement>, IAuthorizationRequirement
    {
        /// <summary>
        /// Function that is called to handle this requirement
        /// </summary>
        public Func<AuthorizationContext, bool> Handler { get; }

        public AssertionRequirement(Func<AuthorizationContext, bool> assert)
        {
            if (assert == null)
            {
                throw new ArgumentNullException(nameof(assert));
            }

            Handler = assert;
        }

        protected override void Handle(AuthorizationContext context, AssertionRequirement requirement)
        {
            if (Handler(context))
            {
                context.Succeed(requirement);
            }
        }
    }
}
