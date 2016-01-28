// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    public class AssertionRequirement : IAuthorizationHandler, IAuthorizationRequirement
    {
        /// <summary>
        /// Function that is called to handle this requirement
        /// </summary>
        public Func<AuthorizationContext, Task<bool>> Handler { get; }

        public AssertionRequirement(Func<AuthorizationContext, bool> assert)
        {
            if (assert == null)
            {
                throw new ArgumentNullException(nameof(assert));
            }

            Handler = context => Task.FromResult(assert(context));
        }

        public AssertionRequirement(Func<AuthorizationContext, Task<bool>> assert)
        {
            if (assert == null)
            {
                throw new ArgumentNullException(nameof(assert));
            }

            Handler = assert;
        }

        public async Task HandleAsync(AuthorizationContext context)
        {
            if (await Handler(context))
            {
                context.Succeed(this);
            }
        }
    }
}
