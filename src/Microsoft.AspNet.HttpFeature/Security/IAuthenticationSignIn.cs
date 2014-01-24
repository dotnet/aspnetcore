using System.Collections.Generic;
#if NET45
using System.Security.Claims;
#endif

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthenticationSignIn
    {
#if NET45
        ClaimsPrincipal User { get; }
#endif
        IDictionary<string, string> Properties { get; }
    }
}