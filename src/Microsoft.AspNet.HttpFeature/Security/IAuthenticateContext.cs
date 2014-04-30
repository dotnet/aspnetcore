using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface IAuthenticateContext
    {
        IList<string> AuthenticationTypes { get; }

        void Authenticated(ClaimsIdentity identity, IDictionary<string, string> properties, IDictionary<string, object> description);

        void NotAuthenticated(string authenticationType, IDictionary<string, string> properties, IDictionary<string, object> description);
    }
}
