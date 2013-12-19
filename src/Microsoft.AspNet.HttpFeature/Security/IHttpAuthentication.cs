using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IHttpAuthentication
    {
        IPrincipal User { get; set; }

        IEnumerable<IAuthenticationResult> Authenticate(string[] authenticationTypes);
        Task<IEnumerable<IAuthenticationResult>> AuthenticateAsync(string[] authenticationTypes);

        IAuthenticationChallenge ChallengeDetails { get; set; }
        IAuthenticationSignIn SignInDetails { get; set; }
        IAuthenticationSignOut SignOutDetails { get; set; }
    }
}