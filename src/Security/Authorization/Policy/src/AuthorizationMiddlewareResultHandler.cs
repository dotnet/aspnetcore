using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization.Policy
{
    /// <summary>
    /// Default implementation for <see cref="IAuthorizationMiddlewareResultHandler"/>.
    /// </summary>
    public class AuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        /// <inheritdoc />
        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Challenged)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }

                return;
            }
            else if (authorizeResult.Forbidden)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ForbidAsync(scheme);
                    }
                }
                else
                {
                    await context.ForbidAsync();
                }

                return;
            }

            await next(context);
        }
    }
}
