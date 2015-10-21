// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authorization.Infrastructure
{
    public class PassThroughAuthorizationHandler : IAuthorizationHandler
    {
        public async Task HandleAsync(AuthorizationContext context)
        {
            foreach (var handler in context.Requirements.OfType<IAuthorizationHandler>())
            {
                await handler.HandleAsync(context);
            }
        }
    }
}
