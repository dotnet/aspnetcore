using System.Security.Claims;
using Microsoft.AspNet.Http.Security;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public class SignInIdentityContext
    {
        public SignInIdentityContext(ClaimsIdentity identity, AuthenticationProperties properties)
        {
            Identity = identity;
            Properties = properties;
        }

        public ClaimsIdentity Identity { get; private set; }
        public AuthenticationProperties Properties { get; private set; }
    }
}
