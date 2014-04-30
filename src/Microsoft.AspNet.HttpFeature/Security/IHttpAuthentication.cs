using System.Security.Claims;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface IHttpAuthentication
    {
        ClaimsPrincipal User { get; set; }
        IAuthenticationHandler Handler { get; set; }
    }
}