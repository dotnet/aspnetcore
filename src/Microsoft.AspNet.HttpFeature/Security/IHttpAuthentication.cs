using System.Security.Claims;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IHttpAuthentication
    {
        ClaimsPrincipal User { get; set; }
        IAuthenticationHandler Handler { get; set; }
    }
}