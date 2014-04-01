using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthenticationHandler
    {
        void GetDescriptions(IAuthTypeContext context);

        void Authenticate(IAuthenticateContext context);
        Task AuthenticateAsync(IAuthenticateContext context);

        void Challenge(IChallengeContext context);
        void SignIn(ISignInContext context);
        void SignOut(ISignOutContext context);
    }
}
