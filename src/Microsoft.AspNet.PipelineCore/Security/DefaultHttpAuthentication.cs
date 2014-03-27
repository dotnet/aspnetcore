using System.Security.Claims;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class DefaultHttpAuthentication : IHttpAuthentication
    {
        public DefaultHttpAuthentication()
        {
        }

        public ClaimsPrincipal User
        {
            get;
            set;
        }

        public IAuthenticationHandler Handler
        {
            get;
            set;
        }
    }
}
