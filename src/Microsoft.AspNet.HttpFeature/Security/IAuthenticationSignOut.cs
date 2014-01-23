using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthenticationSignOut
    {
        IEnumerable<string> AuthenticationTypes { get; }
    }
}