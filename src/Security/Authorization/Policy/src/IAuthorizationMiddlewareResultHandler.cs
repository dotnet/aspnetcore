using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization
{
    public interface IAuthorizationMiddlewareResultHandler
    {
        Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult);
    }
}
