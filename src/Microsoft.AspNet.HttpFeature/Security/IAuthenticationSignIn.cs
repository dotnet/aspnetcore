using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IAuthenticationSignIn
    {
        ClaimsPrincipal User { get; }
        IDictionary<string, string> Properties { get; }
    }
}