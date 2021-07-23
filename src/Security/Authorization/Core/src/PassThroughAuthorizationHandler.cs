// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    /// <summary>
    /// Infrastructure class which allows an <see cref="IAuthorizationRequirement"/> to
    /// be its own <see cref="IAuthorizationHandler"/>.
    /// </summary>
    public class PassThroughAuthorizationHandler : IAuthorizationHandler
    {
        /// <summary>
        /// Makes a decision if authorization is allowed.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var handler in context.Requirements.OfType<IAuthorizationHandler>())
            {
                await handler.HandleAsync(context);
            }
        }
    }
}
