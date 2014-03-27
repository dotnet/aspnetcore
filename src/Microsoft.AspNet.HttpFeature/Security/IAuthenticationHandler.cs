using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public delegate void DescriptionDelegate(IDictionary<string, object> description, object state);

    public interface IAuthenticationHandler
    {
        void GetDescriptions(DescriptionDelegate callback, object state);

        void Authenticate(IAuthenticateContext context); // TODO: (maybe?)
        Task AuthenticateAsync(IAuthenticateContext context);

        void Challenge(IChallengeContext context);
        void SignIn(ISignInContext context);
        void SignOut(ISignOutContext context);
    }
}
