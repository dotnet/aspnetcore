using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface ISignInContext
    {
        IList<ClaimsIdentity> Identities { get; }
        IDictionary<string, string> Properties { get; }

        void Accept(string authenticationType, IDictionary<string, object> description);
    }
}