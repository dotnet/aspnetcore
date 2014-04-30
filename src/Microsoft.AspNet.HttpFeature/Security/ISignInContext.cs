using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface ISignInContext
    {
        IList<ClaimsIdentity> Identities { get; }
        IDictionary<string, string> Properties { get; }

        void Accept(string authenticationType, IDictionary<string, object> description);
    }
}